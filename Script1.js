// JavaScript source code
// RestoranOtomasyonuBackend/src/server.js
const express = require('express');
const cors = require('cors');
const dotenv = require('dotenv');

dotenv.config();

const app = express();
const PORT = process.env.PORT || 5000;

// Middleware
app.use(cors({
  origin: ['http://localhost:3000', 'http://localhost:3001'],
  methods: ['GET', 'POST', 'PUT', 'DELETE'],
  allowedHeaders: ['Content-Type', 'Authorization']
}));
app.use(express.json());

// ============ SAHTE VERÝTABANI ============
const users = [
  { id: 1, email: 'admin@restoran.com', password: 'admin123', role: 'admin', name: 'Admin' },
  { id: 2, email: 'garson@restoran.com', password: 'garson123', role: 'garson', name: 'Garson' },
  { id: 3, email: 'asci@restoran.com', password: 'asci123', role: 'asci', name: 'Aþçý' },
  { id: 4, email: 'kurye@restoran.com', password: 'kurye123', role: 'kurye', name: 'Kurye' }
];

const orders = [
  { siparisId: 1, uyeAdi: 'Ayþe Yýlmaz', toplamTutar: 340, siparisDurumu: 'HAZIR', siparisTipi: 'SALON', detaySayisi: 3 },
  { siparisId: 2, uyeAdi: 'Burak Demir', toplamTutar: 520, siparisDurumu: 'BEKLEMEDE', siparisTipi: 'PAKET', detaySayisi: 3 },
  { siparisId: 3, uyeAdi: 'Ceren Öztürk', toplamTutar: 280, siparisDurumu: 'HAZIRLANIYOR', siparisTipi: 'SALON', detaySayisi: 3 },
  { siparisId: 4, uyeAdi: 'Mehmet Kaya', toplamTutar: 450, siparisDurumu: 'TAMAMLANDI', siparisTipi: 'SALON', detaySayisi: 4 }
];

const products = [
  { id: 1, urunAdi: 'Adana Kebap', fiyat: 120, kategori: 'Kebaplar' },
  { id: 2, urunAdi: 'Lahmacun', fiyat: 70, kategori: 'Hamur Ýþleri' },
  { id: 3, urunAdi: 'Künefe', fiyat: 85, kategori: 'Tatlýlar' }
];

const tables = [
  { masaId: 1, masaNo: 'Masa 01', masaDurumu: 'BOÞ', kapasite: 4 },
  { masaId: 2, masaNo: 'Masa 02', masaDurumu: 'DOLU', kapasite: 4 },
  { masaId: 3, masaNo: 'Masa 03', masaDurumu: 'BOÞ', kapasite: 2 },
  { masaId: 4, masaNo: 'Masa 04', masaDurumu: 'REZERVE', kapasite: 3 }
];

const notifications = [
  { id: 1, type: 'stock_order', message: 'Aþçý Ahmet tarafýndan 50 kg un sipariþ edildi.', read: false, time: '5 dk önce' },
  { id: 2, type: 'stock_alert', message: 'Domates stokta bitmek üzere! (10 kg kaldý)', read: false, time: '15 dk önce' }
];

// ============ AUTH ENDPOINT'LERÝ ============

// Login
app.post('/api/Auth/login', (req, res) => {
  const { email, password } = req.body;
  
  const user = users.find(u => u.email === email && u.password === password);
  
  if (user) {
    res.json({
      success: true,
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
        role: user.role
      }
    });
  } else {
    res.status(401).json({
      success: false,
      message: 'Email veya þifre hatalý!'
    });
  }
});

// ============ SÝPARÝÞ ENDPOINT'LERÝ ============

// Tüm sipariþler
app.get('/api/siparisler', (req, res) => {
  res.json(orders);
});

// Sipariþ detayý
app.get('/api/siparisler/:id', (req, res) => {
  const id = parseInt(req.params.id);
  const order = orders.find(o => o.siparisId === id);
  
  if (order) {
    res.json(order);
  } else {
    res.status(404).json({ message: 'Sipariþ bulunamadý!' });
  }
});

// Sipariþ durumu güncelle
app.put('/api/siparisler/:id/durum', (req, res) => {
  const id = parseInt(req.params.id);
  const { siparisDurumu } = req.body;
  
  const order = orders.find(o => o.siparisId === id);
  if (order) {
    order.siparisDurumu = siparisDurumu;
    res.json({ success: true, message: `Sipariþ #${id} durumu: ${siparisDurumu}` });
  } else {
    res.status(404).json({ message: 'Sipariþ bulunamadý!' });
  }
});

// Sipariþ iptal
app.put('/api/siparisler/:id/iptal', (req, res) => {
  const id = parseInt(req.params.id);
  const order = orders.find(o => o.siparisId === id);
  
  if (order) {
    order.siparisDurumu = 'IPTAL';
    res.json({ success: true, message: `Sipariþ #${id} iptal edildi!` });
  } else {
    res.status(404).json({ message: 'Sipariþ bulunamadý!' });
  }
});

// ============ ÜRÜN ENDPOINT'LERÝ ============

// Tüm ürünler
app.get('/api/Urunler', (req, res) => {
  res.json(products);
});

// Ürün ekle
app.post('/api/Urunler', (req, res) => {
  const { urunAdi, fiyat, kategori } = req.body;
  const newProduct = {
    id: products.length + 1,
    urunAdi,
    fiyat,
    kategori
  };
  products.push(newProduct);
  res.json({ success: true, product: newProduct });
});

// ============ MASA ENDPOINT'LERÝ ============

// Tüm masalar
app.get('/api/Masa', (req, res) => {
  res.json(tables);
});

// ============ BÝLDÝRÝM ENDPOINT'LERÝ ============

// Tüm bildirimler
app.get('/api/bildirimler', (req, res) => {
  res.json(notifications);
});

// ============ SUNUCUYU BAÞLAT ============
app.listen(PORT, () => {
  console.log(`?? Server çalýþýyor: http://localhost:${PORT}`);
  console.log(`?? API adresleri:`);
  console.log(`   POST /api/Auth/login    - Giriþ yap`);
  console.log(`   GET  /api/siparisler    - Tüm sipariþler`);
  console.log(`   GET  /api/Urunler       - Tüm ürünler`);
  console.log(`   GET  /api/Masa          - Tüm masalar`);
  console.log(`   GET  /api/bildirimler   - Bildirimler`);
});