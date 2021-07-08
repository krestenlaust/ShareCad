using Ptc.Controls.Core;
using Ptc.Controls.Text;
using Ptc.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ptc.Controls.Core.Serialization;

namespace ShareCad
{
    [Obsolete("Ved ikke om der kan laves videre på dette, før vi har en måde at serialisere enkelte regions ad gangen.")]
    public static class ManipulateSingleRegionsOld
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
                int? hashCode = GenerateRegionHash(control);

                // check if element type is supported.
                if (!hashCode.HasValue)
                {
                    Console.WriteLine($"{control.GetType()} is not supported.");
                    return;
                }

                // element is supported.
                hashBycontrols[control] = hashCode.Value;
                controlID = controlByID.Count;

                controlByID[controlID] = control;
            }

            // should never return null since control has succeeded before.
            int? newHash = GenerateRegionHash(control);

            if (newHash.Value == previousHash)
            {
                Console.WriteLine("Hash, hasn't changed, no need to notify remote.");
                return;
            }

            // update hash.
            hashBycontrols[control] = newHash.Value;

            // netcode-ish. Notify remote.
            // todo: ...
        }

        /// <summary>
        /// Called when a control is updated remotely and needs to be changed locally accordingly.
        /// </summary>
        /// <param name="dto"></param>
        public static void UpdateControlRemote(IWhiteboardItem controlToUpdate)
        {
            /*
            if (!controlByID.TryGetValue(controlToUpdate.ichr, out IWhiteboardItem control))
            {
                // control doesn't exist already, instantiate new.
                // ...

            }*/
        }

        /// <summary>
        /// Generate an identity for a given control.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static int? GenerateRegionHash(IWhiteboardItem item)
        {
            StringBuilder regionIdentity = new StringBuilder();
            regionIdentity.Append(item.GetType());
            regionIdentity.Append(item);
            regionIdentity.Append(item.VerticalAlignmentOffset);

            // add region specific attributes.
            switch (item)
            {
                case TextRegion textRegion:
                    Console.WriteLine("!!!Textregion");
                    regionIdentity.Append(textRegion.Height);
                    regionIdentity.Append(textRegion.Width);
                    regionIdentity.Append(textRegion.Tag);
                    break;
                case EquationControl equationRegion:
                    Console.WriteLine("!!!EqRegion");
                    break;
                default:
                    break;
            }

            return regionIdentity.ToString().GetHashCode();
        }
    }
}
