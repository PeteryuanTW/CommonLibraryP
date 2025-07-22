using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.LogPKG
{
    public interface ISerilogLogViewer
    {
        Task ReadSerilogData();
    }
}
