
using Microsoft.JSInterop;
using System;
using System.Net.Http.Json;
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
        
        List<UserRecord>? Users = new List<UserRecord>();

        public Dictionary<string, List<Impianto>>? Impianti;

        public string ?Error;

        private readonly IJSRuntime JS;

        public GlobalService(HttpClient http, IJSRuntime js) { Http = http; JS = js; Error = null; }

        public async Task<List<UserRecord>?> GetUsers()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // timeout desiderato
                Users = await Http.GetFromJsonAsync<List<UserRecord>>( ProxyBase + UsersFileName, cancellationToken: cts.Token );          
                
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Caso: file inesistente
                Error = "Il file utenti non esiste. Creare manualmente almeno un account amministratore.";
                Users = null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == null)
            {
                // Caso: rete non disponibile, DNS, proxy, timeout
                Error = "Errore di rete: impossibile raggiungere il server.";
                Users = null;
            }
            /*
            catch (HttpRequestException)
            {
                // Errori di rete/HTTP (5xx, 4xx, DNS, ecc.)
                Users = null;
            }
            catch (OperationCanceledException)
            {
                // Timeout o cancellazione manuale
                // Mostra un messaggio all’utente / fai logging
                Error = "Errore Timeout o cancellazione manuale.";
                Users = null;
            }           
            catch (NotSupportedException)
            {
                // Content-Type non JSON o formati non supportati
                Users = null;
            }
            catch (JsonException)
            {
                // JSON malformato o non coerente con UserRecord
                Users = null;
            }
            */
            catch (Exception ex)
            {
                // Caso: rete non disponibile, DNS, proxy, timeout
                Error = ex.Message;
                Users = null;
            }
            
            return Users;
        }

        public async Task<Dictionary<string, List<Impianto>>?> GetInstallations()
        {
            try
            {
                Impianti = await Http.GetFromJsonAsync< Dictionary<string, List<Impianto>> >(ProxyBase + InstallationFileName);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Caso: file inesistente
                Error = "Il file utenti non esiste. Creare manualmente almeno un account amministratore.";
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

        /// <summary>
        /// memoria del browser
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task LoggedUser(string user)
        {
            await JS.InvokeVoidAsync("sessionStorage.setItem", "loggedUser", user);
            User = user;
        }
        public async Task LoggedRole(string role)
        {
            await JS.InvokeVoidAsync("sessionStorage.setItem", "loggedRole", role);
            Role = role;
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
