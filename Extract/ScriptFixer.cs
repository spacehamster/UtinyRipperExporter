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
			Util.DeleteDirectory($"{csharp}/Kingmaker/EntitySystem/Persistence/JsonUtility");
			Util.DeleteDirectory($"{ExportPath}/Assets/Scripts/Newtonsoft.Json");
		}
	}
}
