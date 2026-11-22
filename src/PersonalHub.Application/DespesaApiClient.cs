using PersonalHub.Application.CasosUso;
using System.Net.Http.Json;

namespace PersonalHub.Application
{
    public class DespesaApiClient 
    {
        private readonly HttpClient _httpClient;
        public DespesaApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DespesaViewModel?> ObterDespesaViewModel(int id)
        {
            _httpClient.BaseAddress = new Uri("https://localhost:7290/api/v1/despesa/" + id.ToString());
            var resposta = await _httpClient.GetAsync(String.Empty);

            var teste = await resposta.Content.ReadAsStringAsync();
            DespesaViewModel? model = await resposta.Content.ReadFromJsonAsync<DespesaViewModel>();
            return model;  
        }

        public async Task<Resposta?> Deletar(int id)
        {
            _httpClient.BaseAddress = new Uri("https://localhost:7290/api/v1/despesa/" + id.ToString());
            var respostaHttp = await _httpClient.DeleteAsync(id.ToString());
            Resposta? resposta = await respostaHttp.Content.ReadFromJsonAsync<Resposta>();
            return resposta;
        }
    }
}
