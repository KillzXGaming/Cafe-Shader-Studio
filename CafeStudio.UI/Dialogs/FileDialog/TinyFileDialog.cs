using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Toolbox.Core;

namespace CafeStudio.UI
{
    public class TinyFileDialog
    {
        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_openFileDialog(string aTitle,
            string aDefaultPathAndFile,
            int aNumOfFilterPatterns,
            string[] aFilterPatterns,
            string aSingleFilterDescription,
            int aAllowMultipleSelects);

        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_saveFileDialog(string aTitle,
            string aDefaultPathAndFile,
            int aNumOfFilterPatterns,
            string[] aFilterPatterns,
            string aSingleFilterDescription);

        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr tinyfd_selectFolderDialog(string aTitle, string aDefaultPathAndFile);

        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_notifyPopup(string aTitle, string aMessage, string aIconType);
        [DllImport("tinyfiledialogs", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tinyfd_messageBox(string aTitle, string aMessage, string aDialogTyle, string aIconType, int aDefaultButton);

        public static string OpenFileDialog(List<FileFilter> filters, string fileName, bool multiSelect)
        {
            string[] filterList = toFilterArray(filters);
            return stringFromAnsi(tinyfd_openFileDialog("Open File", fileName, filterList.Length, filterList, "", multiSelect ? 1 : 0)); ;
        }

        public static string SaveFileDialog(List<FileFilter> filters, string fileName)
        {
            string[] filterList = toFilterArray(filters);
            return stringFromAnsi(tinyfd_saveFileDialog("Save File", fileName, filterList.Length, filterList, "")); ;
        }

        public static string SelectFolderDialog(string title, string defaultPathAndFile)
        {
            return stringFromAnsi(tinyfd_selectFolderDialog(title, defaultPathAndFile));
        }

        public static int MessageBoxInfoOk(string message)
        {
            return tinyfd_messageBox("Cafe Shader Studio", message, "ok", "info", 1);
        }

        private static string[] toFilterArray(List<FileFilter> filters)
        {
            string[] filterList = new string[filters.Count];
            for (int i = 0; i < filters.Count; i++)
                filterList[i] = $"{filters[i].Extension}";
            return filterList;
        }

        // for UTF-8/char
        private static string stringFromAnsi(IntPtr ptr)
        {
            return Marshal.PtrToStringAnsi(ptr);
        }
    }
}
