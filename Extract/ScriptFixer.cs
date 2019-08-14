using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extract
{
	public class ScriptFixer
	{
		public static void FixScripts(string ExportPath)
		{
			FixKingmakerScripts(ExportPath);
		}
		public static void FixKingmakerScripts(string ExportPath)
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
			Util.InsertInFile($"{csharp}/InputModules/PS4InputModule.cs",
				23,
				@"
		public override void Process() { }");
			Util.InsertInFile($"{csharp}/UnityEngine/UI/KingmakerGraphicRaycaster.cs",
	21,
	@"
		public override Camera eventCamera { get { return null; }}
		public override void Raycast(PointerEventData eventData, System.Collections.Generic.List<RaycastResult> resultAppendList){ }");
			Util.DeleteDirectory($"{csharp}/Kingmaker/EntitySystem/Persistence/JsonUtility");
			Util.DeleteDirectory($"{ExportPath}/Assets/Scripts/Newtonsoft.Json");
		}
		public static void FixPoE2Scripts(string ExportPath)
		{
			if (!Directory.Exists(ExportPath))
			{
				throw new Exception($"Directory {ExportPath} doesn't exist");
			}
			var csharp = $"{ExportPath}/Assets/Scripts/Assembly-CSharp";
			if (!Directory.Exists(csharp))
			{
				throw new Exception($"Directory {csharp} doesn't exist");
			}
			Util.ReplaceInFile($"{csharp}/Game/AI/AIBehaviorBundle.cs",
				"public class AIBehaviorBundle : DataBundle<AIBehaviorData>",
				"public class AIBehaviorBundle : DataBundle<OEIFormats.FlowCharts.AI.AIBehaviorData>");
			Util.ReplaceInFile($"{csharp}/Game/AI/AIDecisionTreeBundle.cs",
				"public class AIDecisionTreeBundle : DataBundle<AIDecisionTreeData>",
				"public class AIDecisionTreeBundle : DataBundle<OEIFormats.FlowCharts.AI.AIDecisionTreeData>");
			Util.ReplaceInFile($"{csharp}/Onyx/AIBehaviorData.cs",
			  "public class AIBehaviorData : AIBehaviorData",
			  "public class AIBehaviorData : OEIFormats.FlowCharts.AI.AIBehaviorData");
			Util.ReplaceInFile($"{csharp}/Game/AI/AIDecisionTreeBundle.cs",
				"public class AIDecisionTreeBundle : DataBundle<AIDecisionTreeData>",
				"public class AIDecisionTreeBundle : DataBundle<OEIFormats.FlowCharts.AI.AIDecisionTreeData>");
			Util.ReplaceInFile($"{csharp}/Onyx/AIDecisionTreeData.cs",
				"public class AIDecisionTreeData : AIDecisionTreeData",
				"public class AIDecisionTreeData : OEIFormats.FlowCharts.AI.AIDecisionTreeData");
			Util.ReplaceInFile($"{csharp}/Game/UI/UIOptionsTag.cs",
				"public TacticalMode TacticalMode;",
				"public GameData.TacticalMode TacticalMode;");
			var yieldInstructionInterface = @"
		public override bool keepWaiting
		{
			get
			{
				throw new System.NotImplementedException();
			}
		}";
			Util.InsertInFile($"{csharp}/Game/CustomWaitForSeconds.cs", 6, yieldInstructionInterface);
			Util.InsertInFile($"{csharp}/Game/WaitForCombatStart.cs", 6, yieldInstructionInterface);
			Util.InsertInFile($"{csharp}/Game/WaitForCombatEnd.cs", 6, yieldInstructionInterface);
			Util.InsertInFile($"{csharp}/Game/WaitForCharacterIdle.cs", 6, yieldInstructionInterface);
			Util.InsertInFile($"{csharp}/Game/WaitForCutscene.cs", 6, yieldInstructionInterface);
			Util.InsertInFile($"{csharp}/Game/WaitForSceneLoad.cs", 6, yieldInstructionInterface);
			Util.InsertInFile($"{csharp}/Onyx/AssetBundleRequest.cs", 6, yieldInstructionInterface);
			Util.InsertInFile($"{csharp}/Onyx/WaitForFrames.cs", 6, yieldInstructionInterface);
			var streamInterface = @"
		public override bool CanRead { get { throw new System.NotImplementedException(); } }
		public override bool CanSeek { get { throw new System.NotImplementedException(); } }
		public override bool CanWrite { get { throw new System.NotImplementedException(); } }
		public override long Length { get { throw new System.NotImplementedException(); } }
		public override long Position { get { throw new System.NotImplementedException(); } set { throw new System.NotImplementedException(); } }
		public override void Flush()
		{
			throw new System.NotImplementedException();
		}
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new System.NotImplementedException();
		}
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new System.NotImplementedException();
		}
		public override void SetLength(long value)
		{
			throw new System.NotImplementedException();
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new System.NotImplementedException();
		}";
			Util.InsertInFile($"{csharp}/Ionic/Zip/OffsetStream.cs", 6, streamInterface);
			Util.InsertInFile($"{csharp}/Ionic/Zlib/ZlibStream.cs", 6, streamInterface);
			Util.InsertInFile($"{csharp}/Ionic/Zip/ZipInputStream.cs", 6, streamInterface);
			Util.InsertInFile($"{csharp}/Ionic/Zip/ZipOutputStream.cs", 6, streamInterface);
			Util.InsertInFile($"{csharp}/Ionic/Zip/ZipSegmentedStream.cs", 6, streamInterface);
			Util.InsertInFile($"{csharp}/Ionic/Zlib/DeflateStream.cs", 6, streamInterface);
			Util.InsertInFile($"{csharp}/Ionic/Zlib/GZipStream.cs", 6, streamInterface);
			Util.InsertInFile($"{csharp}/Ionic/Zlib/ZlibBaseStream.cs", 6, streamInterface);
			var stringComparerInterface = @"
		public override int Compare(string x, string y)
		{
			throw new NotImplementedException();
		}
		public override bool Equals(string x, string y)
		{
			throw new NotImplementedException();
		}
		public override int GetHashCode(string obj)
		{
			throw new NotImplementedException();
		}";
			Util.InsertInFile($"{csharp}/Onyx/StringComparerStripDiacriticals.cs", 6, stringComparerInterface);
			var KeyedCollectionInterface = @"
		protected override Type GetKeyForItem(TypeInfo item)
		{
			throw new NotImplementedException();
		}";
			Util.InsertInFile($"{csharp}/Polenter/Serialization/Serializing/TypeInfoCollection.cs", 7, KeyedCollectionInterface);
		}
	}
}
