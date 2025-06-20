using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG.Component
{
    public abstract class WorkorderInfoComponent : ComponentBase
    {
        //station -> workorder -> item -> task 
        [Parameter]
        public Station? StationParamFromRoot { get; set; }

        protected bool hasStation => StationParamFromRoot != null;

        protected bool ToWorkorderLevelValid
            => hasStation;

        protected bool ToItemLevelValid
            => ToWorkorderLevelValid;
    }
}
