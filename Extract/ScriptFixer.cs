using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extract
{
    class ScriptFixer
    {
        public static void FixScripts(string ExportPath)
        {
            var csharp = $"{ExportPath}/Assets/Scripts/Assembly-CSharp";
            var firstpass = $"{ExportPath}/Assets/Scripts/Assembly-CSharp-firstpass";
            Util.ReplaceInFile($"{csharp}/Kingmaker/Visual/CharacterSystem/BodyPartType.cs",
                "enum BodyPartType", "enum BodyPartType : long");
            Util.ReplaceInFile($"{csharp}/Kingmaker/Blueprints/Classes/Spells/SpellDescriptor.cs",
                "enum SpellDescriptor", "enum SpellDescriptor : long");
            Util.ReplaceInFile($"{csharp}/Kingmaker/UI/StaticCanvas.cs",
                "QuestNotification QNotification;", "QuestNotification.QuestNotification QNotification;");
            Util.ReplaceInFile($"{csharp}/Kingmaker/UI/Canvases/BaseGameOverCanvas.cs",
             "SaveLoadWindow m_SaveLoadWindow;", "SaveLoadWindow.SaveLoadWindow m_SaveLoadWindow;");
            Util.ReplaceInFile($"{csharp}/Kingmaker/UI/Vendor/CharDollInTrade.cs",
                "Inventory.EncumbranceContainer CharacterEncumbrance;",
                "ServiceWindow.Inventory.EncumbranceContainer CharacterEncumbrance;");
            Util.ReplaceInFile($"{csharp}/Kingmaker/UI/Loot/LootWindowController.cs",
                "Inventory.EncumbranceContainer m_StashEncumbrance;",
                "ServiceWindow.Inventory.EncumbranceContainer m_StashEncumbrance;");
            Util.ReplaceInFile($"{csharp}/Kingmaker/UI/Vendor/VendorUI.cs",
                "Inventory.EncumbranceContainer PlayerEncumbrance;",
                "ServiceWindow.Inventory.EncumbranceContainer PlayerEncumbrance;");
            Util.ReplaceInFile($"{csharp}/Kingmaker/UI/Vendor/VendorUI.cs",
                "Inventory.EncumbranceContainer StashEncumbrance;",
                "ServiceWindow.Inventory.EncumbranceContainer StashEncumbrance;");
            Util.InsertInFile($"{csharp}/Kingmaker/AreaLogic/Cutscenes/Commands/Timeline/BarkPlayableAsset.cs",
                9,
                @"
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return new Playable();
        }");
            Util.InsertInFile($"{csharp}/Kingmaker/AreaLogic/Cutscenes/Commands/Timeline/BarkPlayableAsset.cs",
                0, "using UnityEngine;");
            Util.InsertInFile($"{csharp}/Kingmaker/View/Equipment/HandEquipmentHelper.cs",
                6,
                @"
        public override bool keepWaiting
        {
            get
            {
                return false;
            }
        }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/CurvedLayout.cs",
                11,
                @"
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/FlowLayoutGroup.cs",
                11,
                @"
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/RadialLayout.cs",
    10,
    @"
        public override void CalculateLayoutInputVertical() { }
        public override void SetLayoutHorizontal() { }
        public override void SetLayoutVertical() { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/CurvedText.cs",
                9,
                @"
        public override void ModifyMesh(VertexHelper vh) { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/CylinderText.cs",
            7,
            @"
        public override void ModifyMesh(VertexHelper vh) { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/Gradient.cs",
                12,
                @"
        public override void ModifyMesh(VertexHelper vh) { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/EventSystems/Extensions/AimerInputModule.cs",
                9,
                @"
        public override void Process() { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/EventSystems/GamePadInputModule.cs",
                18,
                @"
        public override void Process() { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/LetterSpacing.cs",
                9,
                @"
        public override void ModifyMesh(VertexHelper vh) { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/VRInputModule.cs",
                6,
                @"
        public override void Process() { }");
            Util.InsertInFile($"{firstpass}/UnityEngine/UI/Extensions/NicerOutline.cs",
    13,
    @"
        public override void ModifyMesh(VertexHelper vh) { }");
        }
    }
}
