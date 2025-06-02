using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Controls;
using Blazor.Diagrams.Core.Events;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.Positions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CommonLibraryP.MapPKG
{
    public class ResizeControl : ExecutableControl
    {

        public readonly double Radius = 4;
        public double Diameter => Radius * 2;



        public ResizeControl()
        {
            //this.jSRuntime = jSRuntime;
        }
        public override Point? GetPosition(Model model)
        {
            // Fixed at top-right
            var node = (model as NodeModel)!;
            if (node.Size == null)
                return null;

            return node.Position.Add(node.Size.Width - Radius / 2, node.Size.Height - Radius / 2);
        }


        public override async ValueTask OnPointerDown(Diagram diagram, Model model, PointerEventArgs e)
        {
            await Task.CompletedTask;
        }
    }
}
