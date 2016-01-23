using System.Collections.Generic;
using System.Text;
using MsgPack;

namespace Neovim
{
    public enum RedrawMethodType
    {
        Clear,
        Resize,
        UpdateForeground,
        UpdateBackground,
        HighlightSet,
        EolClear,
        SetTitle,
        Put,
        CursorGoto,
        Scroll,
        SetScrollRegion,
        ModeChange,
        BusyStart,
        BusyStop,
        MouseOn,
        MouseOff
    }

    public struct RedrawMethod
    {
        public readonly RedrawMethodType Method;
        public readonly IList<IList<MessagePackObject>> Params;

        public RedrawMethod(RedrawMethodType method, IList<IList<MessagePackObject>> pParams)
        {
            Method = method;
            Params = pParams;
        }
    }

    public static class NotificationParser
    {
        public static IList<RedrawMethod> ParseRedraw(IList<MessagePackObject> pParams)
        {
            var result = new List<RedrawMethod>(pParams.Count);
            foreach (var f in pParams)
            {
                var list = f.AsList();
                string function = list[0].AsString(Encoding.Default);

                IList<IList<MessagePackObject>> args = new List<IList<MessagePackObject>>();
                for (var i = 1; i < list.Count; i++)
                    args.Add(list[i].AsList());

                switch (function)
                {
                    case "clear":
                        result.Add(new RedrawMethod(RedrawMethodType.Clear, args));
                        break;
                    case "resize":
                        result.Add(new RedrawMethod(RedrawMethodType.Resize, args));
                        break;
                    case "update_fg":
                        result.Add(new RedrawMethod(RedrawMethodType.UpdateForeground, args));
                        break;
                    case "update_bg":
                        result.Add(new RedrawMethod(RedrawMethodType.UpdateBackground, args));
                        break;
                    case "highlight_set":
                        result.Add(new RedrawMethod(RedrawMethodType.HighlightSet, args));
                        break;
                    case "eol_clear":
                        result.Add(new RedrawMethod(RedrawMethodType.EolClear, args));
                        break;
                    case "set_title":
                        result.Add(new RedrawMethod(RedrawMethodType.SetTitle, args));
                        break;
                    case "put":
                        result.Add(new RedrawMethod(RedrawMethodType.Put, args));
                        break;
                    case "cursor_goto":
                        result.Add(new RedrawMethod(RedrawMethodType.CursorGoto, args));
                        break;
                    case "scroll":
                        result.Add(new RedrawMethod(RedrawMethodType.Scroll, args));
                        break;
                    case "set_scroll_region":
                        result.Add(new RedrawMethod(RedrawMethodType.SetScrollRegion, args));
                        break;
                    case "mode_change":
                        result.Add(new RedrawMethod(RedrawMethodType.ModeChange, args));
                        break;
                    case "busy_start":
                        result.Add(new RedrawMethod(RedrawMethodType.BusyStart, args));
                        break;
                    case "busy_stop":
                        result.Add(new RedrawMethod(RedrawMethodType.BusyStop, args));
                        break;
                    case "mouse_on":
                        result.Add(new RedrawMethod(RedrawMethodType.MouseOn, args));
                        break;
                    case "mouse_off":
                        result.Add(new RedrawMethod(RedrawMethodType.MouseOff, args));
                        break;
                }
            }
            return result;
        }
    }
}