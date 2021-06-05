using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareCad.Models
{
    public class TextRegionDto : RegionDto
    {
        public string TextContent { get; set; }

        public TextRegionDto(Ptc.Controls.Text.TextRegion region)
        {
            TextContent = region.Text;
        }
    }
}
