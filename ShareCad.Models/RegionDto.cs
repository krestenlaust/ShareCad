using Microsoft.Win32.SafeHandles;
using Ptc.Controls;
using Ptc.Controls.Core;
using Ptc.Controls.Worksheet;
using Ptc.Wpf;
using Ptc.PersistentData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Xml.Serialization;
using static Ptc.Win32.User32;

namespace ShareCad.Models
{
    public class RegionDto
    {
        public POINT GridPosition { get; set; }
    }
}
