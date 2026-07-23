using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restoran.API.Dtos
{
    public class SiparisIadeDto  // ✅ "public" ekledik!
    {
        public int SiparisId { get; set; }
        public string IadeSebebi { get; set; } = null!;
        public int? PersonelId { get; set; }
    }
}
