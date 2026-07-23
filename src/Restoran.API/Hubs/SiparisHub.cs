using Microsoft.AspNetCore.SignalR;

namespace Restoran.API.Hubs;

public class SiparisHub : Hub
{
    // Müşteriyi gruba ekle
    public async Task JoinCustomerGroup(int uyeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"customer_{uyeId}");
    }

    // Sipariş durumu güncellendiğinde bildirim gönder
    public async Task SiparisDurumGuncellendi(int siparisId, string mesaj)
    {
        await Clients.All.SendAsync("SiparisDurumGuncellendi", new { siparisId, mesaj });
    }

    // Belirli bir müşteriye özel bildirim
    public async Task MusteriBildirimGonder(int uyeId, string mesaj)
    {
        await Clients.Group($"customer_{uyeId}").SendAsync("MusteriBildirim", mesaj);
    }
}