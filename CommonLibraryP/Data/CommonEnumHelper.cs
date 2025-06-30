using CommonLibraryP.MachinePKG;
using DevExpress.Blazor;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.Data
{
    public static class CommonEnumHelper
    {
        private static List<StatusDetailWrapperClass> statusDetailWrapperClasses = new()
        {
            new(0, "Init", ButtonRenderStyle.Secondary, Color.FromArgb(143, 143, 143)),
            new(1, "Try Connecting", ButtonRenderStyle.Info, Color.FromArgb(91, 91, 174)),
            new(2, "Disconnect", ButtonRenderStyle.Danger, Color.FromArgb(204, 0, 0)),
            new(3, "Fetching Data", ButtonRenderStyle.Info, Color.FromArgb(130, 192, 192)),
            new(4, "Idle", ButtonRenderStyle.Info, Color.FromArgb(130, 192, 192)),
            new(5, "Running", ButtonRenderStyle.Success, Color.FromArgb(1, 178, 104)),
            new(6, "Pause", ButtonRenderStyle.Warning, Color.FromArgb(235, 192, 0)),
            new(7, "Stop", ButtonRenderStyle.Danger, Color.FromArgb(204, 0, 0)),
            new(8, "Error", ButtonRenderStyle.Danger, Color.FromArgb(204, 0, 0)),
            new(100, "Not Defined", ButtonRenderStyle.Danger, Color.FromArgb(204, 0, 0)),
        };
        /// <summary>
        /// <para>1. Index last than or equal to 100 is reserved for build in status</para>
        /// <para>2. Non-case sensitive status name and index should not be duplicate </para>
        /// </summary>
        public static void AddCustomStatus(int index, string statusName, ButtonRenderStyle buttonRenderStyle, Color color)
        {
            var target = statusDetailWrapperClasses.FirstOrDefault(x => x.Index == index || x.DisplayName == statusName);
            if (target is not null)
            {
                throw new Exception("index or Non-case sensitive name duplicated");
            }
            else
            {
                if (index <= 100)
                {
                    throw new Exception("index last than or equal to 100 is reserved for build in status");
                }
                statusDetailWrapperClasses.Add(new(index, statusName, buttonRenderStyle, color));
            }
        }

        public static StatusDetailWrapperClass GetStatusDetail(int statusCode)
        {
            var target = statusDetailWrapperClasses.FirstOrDefault(x => x.Index == statusCode);
            return target is not null ? target : GetStatusDetail(100);
        }
    }
    public class StatusDetailWrapperClass : EnumWrapper
    {
        public ButtonRenderStyle buttonRenderStyle { get; init; }
        public Color StyleColor { get; init; }
        public string ColorRGBString => $"RGB({StyleColor.R}, {StyleColor.G}, {StyleColor.B})";
        public string ColorHTMLString => $"#{StyleColor.R:X2}{StyleColor.G:X2}{StyleColor.B:X2}";
        public StatusDetailWrapperClass(int index, string displayName, ButtonRenderStyle buttonRenderStyle, Color color)
        {
            this.index = index;
            this.displayName = displayName;
            this.buttonRenderStyle = buttonRenderStyle;
            this.StyleColor = color;
        }
    }
    //public enum Status
    //{
    //    Init,
    //    TryConnecting,
    //    Disconnect,

    //    Idle,
    //    Running,
    //    Pause,
    //    Stop,
    //    Error,
    //}
}
