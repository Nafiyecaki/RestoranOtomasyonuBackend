using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Restoran.API.Hubs
{
    public class SiparisHub : Hub
    {
        // Müşteri login olduğunda kendi UyeId'sine özel gruba katılır
        public async Task JoinCustomerGroup(int uyeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Musteri_{uyeId}");
        }

        // Müşteri tek bir sipariş detay sayfasındaysa o siparişi dinleyebilir
        public async Task JoinOrderGroup(int siparisId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Siparis_{siparisId}");
        }
    }
}