using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresEditor;
using ImGuiNET;
using BfresLibrary;

namespace BfresEditor
{
    public class UserDataInfoEditor
    {
        static List<UserData> Selected = new List<UserData>();
        static bool dialogOpen = false;

        static UserDataDialog ActiveDialog = new UserDataDialog();

        public static void Render(ResDict<UserData> userDataDict)
        {
            if (ImGui.Button("Add"))
            {
                var userData = new UserData();
                userData.Name = " ";
                Selected.Add(userData);

                dialogOpen = true;
                ImGui.OpenPopup("##user_data_dialog");
                if (ActiveDialog.LoadDialog(userData, dialogOpen)) {
                    userDataDict.Add(userData.Name, userData);
                }
            }

            var diabledTextColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            if (Selected.Count == 0)
                ImGui.PushStyleColor(ImGuiCol.Text, diabledTextColor);

            ImGui.SameLine();
            if (ImGui.Button("Remove") && Selected.Count > 0)
            {
                foreach (var usd in Selected)
                    userDataDict.Remove(usd);
            }
            ImGui.SameLine();
            if (ImGui.Button("Edit") && Selected.Count > 0) {
                dialogOpen = true;
                ImGui.OpenPopup("##user_data_dialog");
            }

            if (Selected.Count == 0)
                ImGui.PopStyleColor();

            if (Selected.Count > 0 && dialogOpen) {
                ActiveDialog.LoadDialog(Selected.FirstOrDefault(), dialogOpen);
            }

            RenderHeader();

            if (ImGui.BeginChild("USER_DATA_LIST"))
            {
                int index = 0;
                foreach (var userData in userDataDict.Values)
                {
                    bool isSelected = Selected.Contains(userData);

                    ImGui.Columns(2);
                    if (ImGui.Selectable(userData.Name, isSelected))
                    {
                        Selected.Clear();
                        Selected.Add(userData);
                    }
                    ImGui.NextColumn();
                    ImGui.Text(GetDataString(userData, ","));
                    ImGui.NextColumn();

                    if (isSelected && ImGui.IsMouseDoubleClicked(0)) {
                        dialogOpen = true;
                        ImGui.OpenPopup("##user_data_dialog");
                    }
                    index++;

                    ImGui.Columns(1);
                }
            }
            ImGui.EndChild();
        }

        static void RenderHeader()
        {
            ImGui.Columns(2);
            if (ImGui.Selectable("Name"))
            {

            }
            ImGui.NextColumn();
            if (ImGui.Selectable("Value"))
            {

            }
            ImGui.Separator();
            ImGui.Columns(1);
        }

        static string GetDataString(UserData userData, string seperator = "\n")
        {
            if (userData.Type == UserDataType.Byte)
                return string.Join(seperator, userData.GetValueByteArray());
            else if (userData.Type == UserDataType.Int32)
                return string.Join(seperator, userData.GetValueInt32Array());
            else if (userData.Type == UserDataType.Single)
                return string.Join(seperator, userData.GetValueSingleArray());
            else
                return string.Join(seperator, userData.GetValueStringArray());

        }
    }
}
