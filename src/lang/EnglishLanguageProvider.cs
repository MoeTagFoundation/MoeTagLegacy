namespace MoeTag.Lang
{
    internal class EnglishLanguageProvider : ILanguageProvider
    {
        public string GetLanguageName()
        {
            return "English";
        }

        public string GetLanguageNode(LanguageNodeType node, params string[] args)
        {
            switch(node)
            {
                case LanguageNodeType.SEARCH_BUTTON_TEXT:
                    return $"Search on {args[0]}";
                case LanguageNodeType.SEARCH_BUTTON_TOOLTIP:
                    return "Click to fetch results";
                case LanguageNodeType.SEARCH_BUTTON_STATE:
                    return $"Search State: {args[0]}";

                case LanguageNodeType.SEARCH_INPUT_HINT:
                    return "Type your tags, seperated with spaces";
                case LanguageNodeType.SEARCH_INPUT_LABEL:
                    return "Search";
                case LanguageNodeType.SEARCH_INPUT_TOOLTIP:
                    return "E.g. 1girl highres";

                case LanguageNodeType.NO_PREVIEW_TEXT:
                    return "Sorry, this image cannot be previewed. (Potentially reloading cache)";
                case LanguageNodeType.EMPTY_PREVIEW_TEXT:
                    return "You have no content loaded. Search, browse and click to preview";
                case LanguageNodeType.EMPTY_TAG_TEXT:
                    return "This content has no tags";

                case LanguageNodeType.SECTION_SEARCHSETTINGS:
                    return "Search & Settings";
                case LanguageNodeType.SECTION_PREVIEWINFO:
                    return "Content Details";
                case LanguageNodeType.SECTION_PREVIEW:
                    return "Content Preview";
                case LanguageNodeType.SECTION_CONTENTGRID:
                    return "Browse Content";

                case LanguageNodeType.PAGE_LABEL:
                    return $"Page {args[0]}";

                case LanguageNodeType.LABEL_BYTESREAD:
                    return $"{args[0]} Bytes Read ({args[1]} MB)";
                case LanguageNodeType.BUTTON_CLOSETAB:
                    return "Close Tab (Dispose)";
                case LanguageNodeType.WARNING_NOCONTENT:
                    return "No Content Loaded";

                case LanguageNodeType.TAG_ARTIST:
                    return "Artist";
                case LanguageNodeType.TAG_CHARACTER:
                    return "Character";
                case LanguageNodeType.TAG_COPYRIGHT:
                    return "Copyright";
                case LanguageNodeType.TAG_META:
                    return "Meta";
                case LanguageNodeType.TAG_TAG:
                    return "Tag";

                case LanguageNodeType.TAG_SEARCH_TOOLTIP:
                    return $"Search for {args[0]}";
                case LanguageNodeType.WARNING_NOTAGS:
                    return "This content has no tags";

                case LanguageNodeType.CLIPBOARD_BUTTON_TEXT:
                    return "Copy to Clipboard";
                case LanguageNodeType.BROWSER_BUTTON_TEXT:
                    return "Open in Default Browser";

                case LanguageNodeType.HEADER_SYSTEMINFO:
                    return "System Info";
                case LanguageNodeType.HEADER_SETTINGS:
                    return "General Settings";

                case LanguageNodeType.PLAY_BUTTON:
                    return "Play Content";
                case LanguageNodeType.PAUSE_BUTTON:
                    return "Pause Content";

                default:
                    return "Untranslated Text";
            }
        }
    }
}
