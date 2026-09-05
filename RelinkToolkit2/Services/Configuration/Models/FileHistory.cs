using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Services.Configuration.Models;

public class FileHistory
{
    public List<Uri> RecentlyOpenedFiles { get; set; } = [];
}
