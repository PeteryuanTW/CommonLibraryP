using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG.Component
{
    public abstract class DashboardComponentBase : ComponentBase
    {
        [Parameter]
        public MapComponent MapComponentParam { get; set; } = null!;

        protected string SizePositionStyle => $"position: absolute; left: {MapComponentParam.PositionX * 100:F2}%; top: {MapComponentParam.PositionY * 100:F2}%; width: {MapComponentParam.Width * 100:F2}%; height: {MapComponentParam.Height * 100:F2}%;";

        protected abstract Task BindingToTarget();

    }
}
