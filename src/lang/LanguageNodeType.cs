using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag.Lang
{
    internal enum LanguageNodeType
    {
        SEARCH_BUTTON_TEXT,
        SEARCH_BUTTON_TOOLTIP,

        SEARCH_BUTTON_STATE,

        SEARCH_INPUT_HINT,
        SEARCH_INPUT_LABEL,
        SEARCH_INPUT_TOOLTIP,

        NO_PREVIEW_TEXT,
        EMPTY_PREVIEW_TEXT,
        EMPTY_TAG_TEXT,

        PAGE_LABEL,

        SECTION_SEARCHSETTINGS,
        SECTION_CONTENTGRID,
        SECTION_PREVIEWINFO,
        SECTION_PREVIEW,

        LABEL_BYTESREAD,
        BUTTON_CLOSETAB,
        WARNING_NOCONTENT,

        TAG_ARTIST,
        TAG_CHARACTER,
        TAG_COPYRIGHT,
        TAG_META,
        TAG_TAG,
        TAG_SEARCH_TOOLTIP,
        WARNING_NOTAGS,

        CLIPBOARD_BUTTON_TEXT,
        BROWSER_BUTTON_TEXT,

        HEADER_SYSTEMINFO,
        HEADER_SETTINGS,

        PLAY_BUTTON,
        PAUSE_BUTTON
    }
}
