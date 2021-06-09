using Networking.Models;
using Ptc.Controls.Core;
using Ptc.Controls.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareCad
{
    public static class ManipulateWorksheet
    {
        /// <summary>
        /// Stores hash value for a control to check if it has changed.
        /// Only used locally.
        /// </summary>
        private static readonly Dictionary<IWhiteboardItem, int> hashBycontrols = new Dictionary<IWhiteboardItem, int>();
        /// <summary>
        /// Stores controls by their order of creation (ID).
        /// </summary>
        private static readonly Dictionary<int, IWhiteboardItem> controlByID = new Dictionary<int, IWhiteboardItem>();

        /// <summary>
        /// Called when a control is updated locally and needs to be reflected remotely.
        /// </summary>
        /// <param name="control"></param>
        public static void UpdateControlLocal(IWhiteboardItem control)
        {
            int controlID;

            // Generate hashcode if new control.
            if (!hashBycontrols.TryGetValue(control, out int previousHash))
            {
                // Generate hash for new control.
                int? hashCode = GenerateHashcode(control);

                // check if element type is supported.
                if (!hashCode.HasValue)
                {
                    Console.WriteLine($"{control.GetType()} is not supported.");
                    return;
                }

                // element is supported.
                hashBycontrols[control] = hashCode.Value;
                controlID = controlByID.Count;

                controlByID[controlByID.Count] = control;
            }

            // should never return null since control has succeeded before.
            int? newHash = GenerateHashcode(control);

            if (newHash.Value == previousHash)
            {
                Console.WriteLine("Hash, hasn't changed, no need to notify remote.");
                return;
            }

            // update hash.
            hashBycontrols[control] = newHash.Value;

            // netcode-ish. Notify remote.

        }

        /// <summary>
        /// Called when a control is updated remotely and needs to be changed locally accordingly.
        /// </summary>
        /// <param name="dto"></param>
        public static void UpdateControlRemote(RegionDto dto)
        {
            if (!controlByID.TryGetValue(dto.ID, out IWhiteboardItem control))
            {
                // control doesn't exist already, instantiate new.
                // ...

            }
        }

        private static void DeserializeIntoControl(RegionDto dto, IWhiteboardItem control)
        {

        }

        private static RegionDto SerializeControl(IWhiteboardItem item)
        {
            switch (item)
            {
                case TextRegion textRegion:
                    return new TextRegionDto()
                    {
                        //ID = ,
                        TextContent = textRegion.Text,
                        GridPosition = new System.Windows.Point(2, 2)
                    };
                default:
                    break;
            }

            return null;
        }

        private static int? GenerateHashcode(IWhiteboardItem item)
        {
            int? hashcode = null;

            switch (item)
            {
                case TextRegion region:
                    hashcode = region.Text.GetHashCode();
                    break;
                default:
                    break;
            }

            return hashcode;
        }
    }
}
