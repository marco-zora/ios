
using Microsoft.JSInterop;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlazorApp_IOS.Shared
{
    public class GlobalService
    {

        public string ProxyBase = "https://first.marcomaria-zora.workers.dev/api/dav/UPLOADS/";
        private readonly HttpClient Http;

        public string UsersFileName = "users.json";
        public string InstallationFileName = "impianti.json";
        public string PlaylistsFileName = "playlists.json";
        public string User = "?";
        public string Role = "?";

        // per non ricaricare il file users.json ad ogni chiamata a GetUsers() se non è stato modificato
        private string? _usersETag;
        private DateTimeOffset? _usersLastModified;
        private bool _usersLoaded;



        List<UserRecord>? Users = new List<UserRecord>();

        public Dictionary<string, List<Impianto>>? Impianti;

        public string ?Error;

        private readonly IJSRuntime JS;
        

        public GlobalService(HttpClient http, IJSRuntime js) { Http = http; JS = js; Error = null; }


    public async Task<List<UserRecord>?> GetUsers(bool forceRefresh = false)
    {
        try
        {
            // Se già caricati e non voglio forzare, provo a evitare rete del tutto
            if (!forceRefresh && _usersLoaded && Users is not null)
                return Users;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var url = ProxyBase + UsersFileName;
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            // Conditional GET: se ho ETag o Last-Modified, chiedo "dammi solo se cambiato"
            if (!forceRefresh)
            {
                if (!string.IsNullOrWhiteSpace(_usersETag))
                    req.Headers.TryAddWithoutValidation("If-None-Match", _usersETag);

                if (_usersLastModified.HasValue)
                    req.Headers.IfModifiedSince = _usersLastModified.Value;
            }

            using var res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            // Se non è modificato, riuso la cache in memoria
            if (res.StatusCode == HttpStatusCode.NotModified && Users is not null)
            {
                _usersLoaded = true;
                Error = null;
                return Users;
            }

            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                Error = "Il file utenti non esiste. Creare manualmente almeno un account amministratore.";
                Users = null;
                _usersLoaded = false;
                return null;
            }

            res.EnsureSuccessStatusCode();

            // Aggiorno cache headers (se disponibili)
            if (res.Headers.ETag is not null)
                _usersETag = res.Headers.ETag.Tag;

            if (res.Content.Headers.LastModified.HasValue)
                _usersLastModified = res.Content.Headers.LastModified.Value;

            Users = await res.Content.ReadFromJsonAsync<List<UserRecord>>(cancellationToken: cts.Token);
            Error = null;
            _usersLoaded = true;
            return Users;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            Error = "Errore di rete: impossibile raggiungere il server.";
            Users = null;
            _usersLoaded = false;
            return null;
        }
        catch (HttpRequestException)
        {
            Error = "Errori di rete/HTTP (5xx, 4xx, DNS, ecc.)";
            Users = null;
            _usersLoaded = false;
            return null;
        }
        catch (OperationCanceledException)
        {
            Error = "Errore Timeout o cancellazione manuale.";
            Users = null;
            _usersLoaded = false;
            return null;
        }
    }



    public async Task SaveUsers()
    {
        try
        {
            Error = null;
            Users ??= new();

            var url = ProxyBase + UsersFileName;

            var json = JsonSerializer.Serialize(
                Users,
                new JsonSerializerOptions { WriteIndented = true }
            );

            using var req = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var res = await Http.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                Error = $"Errore salvataggio: {res.StatusCode}";
                return;
            }


            _usersETag = null;
            _usersLastModified = null;
            _usersLoaded = true;   // ho dati validi in memoria

        }
        catch (Exception ex)
        {
            Error = "Errore: " + ex.Message;
        }
    }

        /*
    public async Task<List<UserRecord>?> GetUsers()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // timeout desiderato

                Users = await Http.GetFromJsonAsync<List<UserRecord>>( ProxyBase + UsersFileName, cancellationToken: cts.Token );                          
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Error = "Il file utenti non esiste. Creare manualmente almeno un account amministratore.";
                Users = null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                Error = "Errore di rete: impossibile raggiungere il server.";
                Users = null;
            }            
            catch (HttpRequestException)
            {
                Error = "Errori di rete/HTTP (5xx, 4xx, DNS, ecc.)";
                Users = null;
            }
            catch (OperationCanceledException)
            {                                
                Error = "Errore Timeout o cancellazione manuale.";
                Users = null;
            }           
            catch (NotSupportedException)
            {
                Error = "Content-Type non JSON o formati non supportati";
                Users = null;
            }
            catch (JsonException)
            {
                Error = "JSON malformato o non coerente con UserRecord";
                Users = null;
            }            
            catch (Exception ex)
            {
                Error = ex.Message;
                Users = null;
            }
            
            return Users;
        }
        */

        public async Task<Dictionary<string, List<Impianto>>?> GetInstallations()
        {
            try
            {
                Impianti = await Http.GetFromJsonAsync< Dictionary<string, List<Impianto>> >(ProxyBase + InstallationFileName);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Caso: file inesistente
                Error = "Il file Impianti non esiste.";
                Impianti = null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                // Caso: rete non disponibile, DNS, proxy, timeout
                Error = "Errore di rete: impossibile raggiungere il server.";
                Impianti = null;
            }

            return Impianti;
        }
        
        public async Task<string?> LoggedUser() 
        {            
            return  User = await JS.InvokeAsync<string>("sessionStorage.getItem", "loggedUser");
        }
        public async Task<string?> LoggedRole()
        {
            return Role = await JS.InvokeAsync<string>("sessionStorage.getItem", "loggedRole");
        }

        public event Action? AuthChanged;

        public async Task LoggedUser(string user)
        {
            await JS.InvokeVoidAsync("sessionStorage.setItem", "loggedUser", user);
            User = user;
            AuthChanged?.Invoke();
        }
        public async Task LoggedRole(string role)
        {
            await JS.InvokeVoidAsync("sessionStorage.setItem", "loggedRole", role);
            Role = role;
            AuthChanged?.Invoke();
        }

        public async Task Logout()
        {
            await JS.InvokeVoidAsync("sessionStorage.removeItem", "loggedUser");
            await JS.InvokeVoidAsync("sessionStorage.removeItem", "loggedRole");
            User = "";
            Role = "";
            AuthChanged?.Invoke();
        }

        public string Sha256(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }


    public class UserRecord
    {
        public int Id { get; set; } // ID unico in ordine crescente
        public string name { get; set; } = "";      // il nome dell'utente - user name
        public string user { get; set; } = "";      // user name
        public string hash { get; set; } = "";      // la password viene memorizzaza con hash codificata
        public string role { get; set; } = "user";  // "user" | "admin"
    }


    public class Installation : Dictionary<string, List<Impianto>> { }

    /*
    public class UserImpianti
    {
        public List<Impianto> impianti { get; set; } = new();
    }
    */
    public class Impianto
    {
        public string id { get; set; } = "";
        public string nome { get; set; } = "";
        public string? indirizzo { get; set; }

        // ✅ nuovo campo per lo sfondo card
        public string? colore { get; set; } = "#e3f2fd";

        public List<MonitorItem> monitors { get; set; } = new();
    }

    public class MonitorItem
    {
        public string id { get; set; } = "";
        public string nome { get; set; } = "";
        public string tipo { get; set; } = "";
        public string risoluzione { get; set; } = "";
        public string? playlist { get; set; } = null;

    }
}
