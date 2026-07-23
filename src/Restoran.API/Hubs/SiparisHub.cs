using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

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

    // ========== YENİ EKLENEN METODLAR ==========

    // Kullanıcı bağlandığında rolüne göre gruba ekle
    public override async Task OnConnectedAsync()
    {
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userRole))
        {
            if (userRole == "Garson")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Garsonlar");
                Console.WriteLine($"Garson bağlandı: {Context.ConnectionId}");
            }
            else if (userRole == "Asci" || userRole == "Aşçı")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Asci");
                Console.WriteLine($"Aşçı bağlandı: {Context.ConnectionId}");
            }
            else if (userRole == "Kurye")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Kuryeler");
                Console.WriteLine($"Kurye bağlandı: {Context.ConnectionId}");
            }
            else if (userRole == "Yönetici" || userRole == "Admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Adminler");
                Console.WriteLine($"Yönetici bağlandı: {Context.ConnectionId}");
            }
        }

        await base.OnConnectedAsync();
    }

    // Kullanıcı bağlantıyı kestiğinde
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            Console.WriteLine($"Bağlantı hatayla koptu: {exception.Message}");
        }
        else
        {
            Console.WriteLine($"Bağlantı koptu: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Aşçı'dan garsona sipariş hazır bildirimi
    public async Task SiparisHazirBildirim(int siparisId, string masaNo, string? mesaj = null)
    {
        await Clients.Group("Garsonlar").SendAsync("SiparisHazir", new
        {
            siparisId,
            masaNo,
            mesaj = mesaj ?? $"Sipariş #{siparisId} - Masa {masaNo} hazır!",
            zaman = DateTime.Now
        });
    }

    // Aşçı'dan kuryeye sipariş hazır bildirimi (Online siparişler için)
    public async Task SiparisHazirKuryeBildirim(int siparisId, string adres, string? mesaj = null)
    {
        await Clients.Group("Kuryeler").SendAsync("SiparisHazirKurye", new
        {
            siparisId,
            adres,
            mesaj = mesaj ?? $"Yeni teslimat siparişi #{siparisId} hazır!",
            zaman = DateTime.Now
        });
    }

    // Yeni sipariş geldiğinde aşçılara bildir
    public async Task YeniSiparisBildirim(int siparisId, string masaNo, string siparisTipi)
    {
        await Clients.Group("Asci").SendAsync("YeniSiparis", new
        {
            siparisId,
            masaNo,
            siparisTipi,
            mesaj = $"Yeni sipariş! Masa {masaNo} - {siparisTipi}",
            zaman = DateTime.Now
        });
    }

    // Sipariş durumu değiştiğinde herkese bildir (Admin paneli için)
    public async Task SiparisDurumGuncellemeBildirim(int siparisId, string eskiDurum, string yeniDurum)
    {
        await Clients.All.SendAsync("SiparisDurumGuncelleme", new
        {
            siparisId,
            eskiDurum,
            yeniDurum,
            mesaj = $"Sipariş #{siparisId} durumu: {eskiDurum} → {yeniDurum}",
            zaman = DateTime.Now
        });
    }
}