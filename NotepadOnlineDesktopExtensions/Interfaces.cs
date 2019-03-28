﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NotepadOnlineDesktopExtensions
{
    public interface IExtension
    {
        string Name { get; }
        string Version { get; }
        string Author { get; }
        string Description { get; }
        List<MenuItem> Menu { get; }
        Task OnStart(IApplicationInstance instance);
        Task OnStop();
    }

    public interface IApplicationInstance
    {
        string Text { get; set; }
        string Name { get; }
        void Open(string name);
        void OpenFolder(string path);
    }
}
