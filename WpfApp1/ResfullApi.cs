using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class ResfullApi
    {
        private readonly HttpClient Client;
        public ResfullApi()
        {
            Client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:8080")
            };
           
        }
        public async Task<List<string>> GetRoomsAsync()
        {
            var rooms = await Client.GetFromJsonAsync<List<string>>("api/rooms");
            return  rooms ?? new List<string>();
        }

    }
}
