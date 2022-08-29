namespace MoeTag.Lang
{
    internal class TestLanguageProvider : ILanguageProvider
    {
        public string GetLanguageName()
        {
            return "Test";
        }

        public string GetLanguageNode(LanguageNodeType node, params string[] args)
        {
            switch(node)
            {
                case LanguageNodeType.SEARCH_BUTTON_TEXT:
                    return $"AAAA {args[0]}";
                case LanguageNodeType.SEARCH_BUTTON_TOOLTIP:
                    return "AAAAAA";
                case LanguageNodeType.SEARCH_BUTTON_STATE:
                    return $"AAAAAA: {args[0]}";

                case LanguageNodeType.SEARCH_INPUT_HINT:
                    return "SASAS ASA SA SAS ASA SASA";
                case LanguageNodeType.SEARCH_INPUT_LABEL:
                    return "ASSSA SAS Ah";
                case LanguageNodeType.SEARCH_INPUT_TOOLTIP:
                    return "EFW EFW WEF EFWEW";

                case LanguageNodeType.NO_PREVIEW_TEXT:
                    return "WAD WAD AD ASDAS DASD SA DSA D";
                case LanguageNodeType.EMPTY_PREVIEW_TEXT:
                    return "SDA DS DASD ASD ASD SAD ";
                case LanguageNodeType.EMPTY_TAG_TEXT:
                    return "WDA DWA WAD WADWA WD ";

                case LanguageNodeType.SECTION_SEARCHSETTINGS:
                    return "WAD WDA AWDDW AWAD WAD ";
                case LanguageNodeType.SECTION_PREVIEWINFO:
                    return "WAD WAD DWA SDSASD SA";
                case LanguageNodeType.SECTION_PREVIEW:
                    return "SDA SDA  ASDSAD ASD ";
                case LanguageNodeType.SECTION_CONTENTGRID:
                    return "BASD SAD ASD ASD ASD  ASD";

                case LanguageNodeType.PAGE_LABEL_FORWARDS:
                    return $"SDA DSA SAD  ASD ASD ASD {args[0]}";

                case LanguageNodeType.LABEL_BYTESREAD:
                    return $"{args[0]} ASD SDA ASD SAD ASD  ({args[1]} SDA ASD )";
                case LanguageNodeType.BUTTON_CLOSETAB:
                    return "ASD ASD SD SAD ASD SAD SAGHRT";
                case LanguageNodeType.WARNING_NOCONTENT:
                    return "EWF EW FEWF EWF EWEWFEFEW";

                case LanguageNodeType.TAG_ARTIST:
                    return "GFD DFG ";
                case LanguageNodeType.TAG_CHARACTER:
                    return "DFGFGD";
                case LanguageNodeType.TAG_COPYRIGHT:
                    return "CFGD GFD t";
                case LanguageNodeType.TAG_META:
                    return "MDFGeFGDDFGa";
                case LanguageNodeType.TAG_TAG:
                    return "TaDFGg";

                case LanguageNodeType.TAG_SEARCH_TOOLTIP:
                    return $"FGDDFG DFG GDF {args[0]}";
                case LanguageNodeType.WARNING_NOTAGS:
                    return "TDFG ASD ASD";

                case LanguageNodeType.CLIPBOARD_BUTTON_TEXT:
                    return "ASD SAD";
                case LanguageNodeType.BROWSER_BUTTON_TEXT:
                    return "FDGG FGDFGDFGDFG";

                default:
                    return "Untranslated Text";
            }
        }
    }
}
