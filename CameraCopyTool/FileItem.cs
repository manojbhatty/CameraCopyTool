using System;
using System.Collections.Generic;
using System.Text;

namespace CameraCopyTool
{
    public class FileItem
    {
        public string Name { get; set; }
        public string ModifiedDate { get; set; }
        public bool IsAlreadyCopied { get; set; } = false;
        // Property for displaying name with optional tick
        public string DisplayName => IsAlreadyCopied ? $"✅ {Name}" : Name;
    }

}
