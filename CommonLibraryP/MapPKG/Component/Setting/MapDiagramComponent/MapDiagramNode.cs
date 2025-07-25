using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using DevExpress.XtraSpellChecker.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG
{
    public class MapDiagramNode : NodeModel
    {
        private MapComponent mapComponent = null!;
        public MapComponent MapComponent => mapComponent;

        public double WidthInMap { get; private set; }
        public double HeightInMap { get; private set; }
        public MapDiagramNode(Point? position = null) : base(position)
        {

        }
        public MapDiagramNode(MapComponent mapComponent, double mapWidth, double mapHeight)
        {
            this.mapComponent = mapComponent;
            Position = new(mapComponent.PositionX * mapWidth, mapComponent.PositionY * mapHeight);
            WidthInMap = mapComponent.Width * mapWidth;
            HeightInMap = mapComponent.Height * mapHeight;
        }

        public void UpdateNodeToComponent(double mapWidth, double mapHeight)
        {
            mapComponent.PositionX = Math.Round(Position.X / mapWidth, 3);
            mapComponent.PositionY = Math.Round(Position.Y / mapHeight, 3);
            mapComponent.Width = Math.Round(Size.Width / mapWidth, 3);
            mapComponent.Height = Math.Round(Size.Height / mapHeight, 3);
        }

    }
}
