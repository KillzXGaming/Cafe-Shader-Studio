using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfshaLibrary;
using CafeStudio.UI;

namespace BfresEditor
{
    public class ShaderModelWrapper : IPropertyUI
    {
        public ShaderModel ShaderModel;

        public ShaderModelWrapper(ShaderModel shaderModel) {
            ShaderModel = shaderModel;
        }

        public Type GetTypeUI() => typeof(ShaderModelGUI);

        public void OnLoadUI(object uiInstance)
        {

        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (ShaderModelGUI)uiInstance;
            editor.Render(ShaderModel);
            PrintProgramKeys();
        }

        public void PrintProgramKeys()
        {
            for (int i = 0; i < ShaderModel.ProgramCount; i++)
            {
                if (ImGuiNET.ImGui.CollapsingHeader($"Program_{i}")) {
                    PrintProgramKeys(i);
                }
            }
        }

        public void PrintProgramKeys(int programIndex)
        {
            int numKeysPerProgram = ShaderModel.StaticKeyLength + ShaderModel.DynamicKeyLength;
            int baseIndex = numKeysPerProgram * programIndex;
            for (int j = 0; j < ShaderModel.StaticOptions.Count; j++)
            {
                var option = ShaderModel.StaticOptions[j];
                int choiceIndex = option.GetChoiceIndex(ShaderModel.KeyTable[baseIndex + option.bit32Index]);
                if (choiceIndex > option.choices.Length || choiceIndex == -1)
                    throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                ImGuiNET.ImGui.Text($"{option.Name} choiceIndex {choiceIndex} value {option.ChoiceDict.GetKey(choiceIndex)}");
            }

            for (int j = 0; j < ShaderModel.DynamiOptions.Count; j++)
            {
                var option = ShaderModel.DynamiOptions[j];
                int ind = option.bit32Index - option.keyOffset;
                int choiceIndex = option.GetChoiceIndex(ShaderModel.KeyTable[baseIndex + ShaderModel.StaticKeyLength + ind]);
                if (choiceIndex > option.choices.Length || choiceIndex == -1)
                    throw new Exception($"Invalid choice index in key table! {option.Name} index {choiceIndex}");

                ImGuiNET.ImGui.Text($"{option.Name} value {option.ChoiceDict.GetKey(choiceIndex)}");
            }
        }
    }
}
