using CommonLibraryP.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public static class ShopfloorTypeEnumHelper
    {
        public static IEnumerable<StationTypeWrapperClass> GetStationTypesWrapperClass()
        {
            return Enum.GetValues(typeof(StationType)).OfType<StationType>()
                .Select(x => new StationTypeWrapperClass(x));
        }
    }

    public class StationTypeWrapperClass : EnumWrapper
    {
        public StationTypeWrapperClass(StationType stationType)
        {
            Type = stationType;
            index = (int)Type;
            displayName = Type.ToString();

        }
        public StationType Type { get; init; }

    }

    /// <summary>
    /// first bit: 1->single workorder, 2->multiple workorder
    /// second bit: 1->single item, 2->multiple item, 3->no serial no
    /// </summary>
    public enum StationType
    {
        SingleWorkorderSingleSerial = 11,
        SingleWorkorderMultipleSerials = 12,
        //SingleWorkorderNoSerial = 112,

        //MultipleWorkorderSingleSerial = 3,
        //MultipleWorkorderMutipleSerials = 4,
        //MultipleWorkorderNoSerial = 5,
    }
}
