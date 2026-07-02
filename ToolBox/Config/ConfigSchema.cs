namespace ToolBox.Config
{
    //"""
    //Sub-Header:   //(For sub-menus in editor)//
    //    // Name of option and short explanation (Default: Value) [List, of, all, available, choices]
    //    Option = "value"
    //"""
    internal class Schema
    {
        internal static Dictionary<string, List<string>> GetSchema()
        {
            Dictionary<string, List<string>> configSchemas = [];
			List<string> schemaSteeleTerm = [
            """
            SSH:
                // How to handle verification of Host Keys. (Default: TrustAfterFirstUse) [TrustAfterFirstUse, RejectAll, AllowAll]
                HostKeyVerificationMethod = "TrustAfterFirstUse"
            """
            ];
			//List<string> schemaWrapHDL = [];
			//List<string> schemaToolBox = [];
			//configSchemas.Add("WrapHDL", schemaWrapHDL);
			configSchemas.Add("SteeleTerm", schemaSteeleTerm);
			//configSchemas.Add("ToolBox", schemaToolBox);
			return configSchemas;
        }
    }
    public class SchemaException : Exception
	{
		public SchemaException() { }
		public SchemaException(string message) : base(message) { }
		public override string? StackTrace => null;
	}
}