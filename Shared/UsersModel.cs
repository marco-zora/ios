

namespace BlazorApp_IOS.Shared
{
    public class UserRecord
    {
        public int Id { get; set; } // ID unico in ordine crescente
        public string name { get; set; } = "";      // il nome dell'utente - user name
        public string user { get; set; } = "";      // user name
        public string hash { get; set; } = "";      // la password viene memorizzaza con hash codificata
        public string role { get; set; } = "user";  // "user" | "admin"
    }
}
