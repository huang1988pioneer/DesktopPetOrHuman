namespace DesktopPetOrHuman;

internal static class AppDialogue
{
    public static string For(string characterKey, AppWatchEvent watchEvent)
    {
        return watchEvent.Action == AppWatchAction.Closed
            ? ForClose(characterKey, watchEvent.App)
            : ForOpen(characterKey, watchEvent.App);
    }

    public static string ForOpen(string characterKey, AppLaunch launch)
    {
        var app = launch.DisplayName;
        return launch.Kind switch
        {
            AppKind.Browser => Browser(characterKey, app),
            AppKind.Code => Code(characterKey, app),
            AppKind.Office => Office(characterKey, app),
            AppKind.Chat => Chat(characterKey, app),
            AppKind.Music => Music(characterKey, app),
            AppKind.Video => Video(characterKey, app),
            AppKind.Game => Game(characterKey, app),
            AppKind.Note => Note(characterKey, app),
            AppKind.Terminal => Terminal(characterKey, app),
            AppKind.Utility => Utility(characterKey, app),
            _ => Generic(characterKey, app)
        };
    }

    private static string Browser(string key, string app) => key switch
    {
        "miaobaibai" => $"喵～打開{app}了",
        "miaobubu" => $"布布看到{app}！要看什麼",
        "xiaotu" => $"喔！{app}開了，衝上網",
        "laowangmao" => $"上網也要招財，{app}開好了",
        "fengxiong" => $"{app}開了，先把正事查完",
        "fengge" => $"{app}就緒。別只顧著滑",
        "xiaoying" => $"{app}…先回郵件再看吧",
        "miaoniang" => $"喵娘陪你逛{app}～",
        "tuge" => $"{app}開了！今天查什麼",
        "yamei" => $"你開了{app}，慢慢看喔",
        "yumei" => $"游進{app}看看～",
        "gugugaga" => $"ググ！{app}打開了",
        _ => $"你打開了{app}"
    };

    private static string Code(string key, string app) => key switch
    {
        "miaobaibai" => $"白白陪你寫程式喔",
        "miaobubu" => $"布布看你敲鍵盤！",
        "xiaotu" => $"{app}登場，來寫扣！",
        "laowangmao" => $"寫出會賺錢的程式！",
        "fengxiong" => $"{app}開了，慢慢寫別爆肝",
        "fengge" => $"{app}就緒，開始幹活",
        "xiaoying" => $"{app}又開了…今天也加班嗎",
        "miaoniang" => $"喵娘當你的程式吉祥物",
        "tuge" => $"{app}！今天也要一次過",
        "yamei" => $"寫程式記得休息喔",
        "yumei" => $"程式像海浪，一波一波寫",
        "gugugaga" => $"ガガ，開始寫 code 了",
        _ => $"你打開了{app}"
    };

    private static string Office(string key, string app) => key switch
    {
        "miaobaibai" => $"要辦公了嗎？白白陪著",
        "miaobubu" => $"{app}！布布當你秘書",
        "xiaotu" => $"拿出{app}，認真模式",
        "laowangmao" => $"{app}開了，業績會來的",
        "fengxiong" => $"{app}開了，一份一份來",
        "fengge" => $"{app}就緒。把報告收尾",
        "xiaoying" => $"{app}…這份做完就能下班嗎",
        "miaoniang" => $"喵娘幫你盯著{app}",
        "tuge" => $"{app}開了！今天也超有效率",
        "yamei" => $"慢慢做{app}，別急",
        "yumei" => $"文件像潮水，一頁一頁來",
        "gugugaga" => $"ググ，{app}辦公時間",
        _ => $"你打開了{app}"
    };

    private static string Chat(string key, string app) => key switch
    {
        "miaobaibai" => $"有人找你了嗎？",
        "miaobubu" => $"{app}亮了！誰找布布的主人",
        "xiaotu" => $"{app}來訊息了？快看",
        "laowangmao" => $"貴人來電？{app}開了",
        "fengxiong" => $"{app}開了，回完再忙",
        "fengge" => $"{app}。先處理重要的",
        "xiaoying" => $"又是{app}…會議是不是又來了",
        "miaoniang" => $"喵～誰在{app}找你",
        "tuge" => $"{app}！我先不當電燈泡",
        "yamei" => $"有訊息的話，溫柔回喔",
        "yumei" => $"海的另一邊有人叫你",
        "gugugaga" => $"ガガ，{app}有人說話",
        _ => $"你打開了{app}"
    };

    private static string Music(string key, string app) => key switch
    {
        "miaobaibai" => $"放歌了喵～白白搖尾巴",
        "miaobubu" => $"音樂！布布要跟著跳",
        "xiaotu" => $"{app}開了，今日 BGM 是？",
        "laowangmao" => $"放財神歌也行，{app}讚",
        "fengxiong" => $"有音樂比較能專心",
        "fengge" => $"放歌可以，音量別吵到開會",
        "xiaoying" => $"有歌聽，加班比較撐得住",
        "miaoniang" => $"喵娘想聽情歌～",
        "tuge" => $"BGM 開了！狀態拉滿",
        "yamei" => $"輕輕放音樂，心情會好",
        "yumei" => $"像海浪的節奏～",
        "gugugaga" => $"ググガガ～放歌了",
        _ => $"你打開了{app}"
    };

    private static string Video(string key, string app) => key switch
    {
        "miaobaibai" => $"要看影片嗎？白白也可以看",
        "miaobubu" => $"影片時間！布布坐好了",
        "xiaotu" => $"{app}！今天看什麼",
        "laowangmao" => $"看片可以，別忘了正事發財",
        "fengxiong" => $"看一下就好，記得休息眼睛",
        "fengge" => $"{app}開了。看完回來工作",
        "xiaoying" => $"用{app}偷閒一下…可以嗎",
        "miaoniang" => $"陪你追劇喵～",
        "tuge" => $"{app}啟動！不要一直看廣告",
        "yamei" => $"看影片也要眨眼喔",
        "yumei" => $"螢幕裡也有一片海",
        "gugugaga" => $"ガガ，要看片了",
        _ => $"你打開了{app}"
    };

    private static string Game(string key, string app) => key switch
    {
        "miaobaibai" => $"要玩{app}了？記得休息喵",
        "miaobubu" => $"遊戲！布布幫你加油",
        "xiaotu" => $"{app}開了！今天要贏",
        "laowangmao" => $"遊戲也求一個大爆金",
        "fengxiong" => $"玩一下可以，別太晚",
        "fengge" => $"{app}。玩完把正事收尾",
        "xiaoying" => $"下班了才准開{app}…對吧",
        "miaoniang" => $"喵娘當你的幸運符～",
        "tuge" => $"{app}！上了就別坑隊友",
        "yamei" => $"玩歸玩，別熬夜喔",
        "yumei" => $"去冒險吧，我會在岸邊等",
        "gugugaga" => $"ググ！要去冒險了",
        _ => $"你打開了{app}"
    };

    private static string Note(string key, string app) => key switch
    {
        "miaobaibai" => $"要記什麼嗎？白白看著",
        "miaobubu" => $"{app}！布布想貼小貼紙",
        "xiaotu" => $"拿出{app}，寫重點",
        "laowangmao" => $"好點子先記下來，以後會值錢",
        "fengxiong" => $"{app}開了，想到就寫",
        "fengge" => $"把待辦記清楚",
        "xiaoying" => $"又要記一堆…先寫最急的",
        "miaoniang" => $"喵娘幫你記住～",
        "tuge" => $"{app}！重點畫起來",
        "yamei" => $"慢慢寫，字漂亮一點",
        "yumei" => $"把心事寫在貝殼上",
        "gugugaga" => $"ググ，要寫字了",
        _ => $"你打開了{app}"
    };

    private static string Terminal(string key, string app) => key switch
    {
        "miaobaibai" => $"黑黑的視窗…白白有點怕",
        "miaobubu" => $"好多字！布布看不懂",
        "xiaotu" => $"終端機開了，駭客模式？",
        "laowangmao" => $"指令下對，財運也會順",
        "fengxiong" => $"終端機開了，小心別下錯指令",
        "fengge" => $"{app}就緒。看清楚再按 Enter",
        "xiaoying" => $"黑畫面…拜託不要報錯",
        "miaoniang" => $"喵？這是在施咒嗎",
        "tuge" => $"終端機！今天一次跑過",
        "yamei" => $"慢慢打指令，別急",
        "yumei" => $"深海一樣的黑畫面呢",
        "gugugaga" => $"ガガ，黑窗口出現了",
        _ => $"你打開了{app}"
    };

    private static string Utility(string key, string app) => key switch
    {
        "miaobaibai" => $"打開{app}了喵",
        "miaobubu" => $"{app}！布布也要玩",
        "xiaotu" => $"{app}出來了",
        "laowangmao" => $"{app}開了，辦事順利",
        "fengxiong" => $"{app}，用完就好",
        "fengge" => $"{app}開了。辦完關掉",
        "xiaoying" => $"{app}…又要處理雜事了",
        "miaoniang" => $"喵娘看到{app}了",
        "tuge" => $"{app}！速戰速決",
        "yamei" => $"你開了{app}喔",
        "yumei" => $"{app}漂過來了",
        "gugugaga" => $"ググ，{app}出現了",
        _ => $"你打開了{app}"
    };

    private static string Generic(string key, string app) => key switch
    {
        "miaobaibai" => $"你打開了{app}喵",
        "miaobubu" => $"新程式！{app}",
        "xiaotu" => $"喔喔，{app}開了",
        "laowangmao" => $"{app}開了，萬事順心",
        "fengxiong" => $"看到你打開{app}了",
        "fengge" => $"{app}已開啟",
        "xiaoying" => $"又開一個…{app}",
        "miaoniang" => $"喵？這是{app}耶",
        "tuge" => $"{app}登場！",
        "yamei" => $"你開了{app}呢",
        "yumei" => $"撲通，{app}游過來了",
        "gugugaga" => $"ガガ，{app}打開了",
        _ => $"你打開了{app}"
    };

    private static string ForClose(string characterKey, AppLaunch launch)
    {
        var app = launch.DisplayName;
        return launch.Kind switch
        {
            AppKind.Browser => CloseBrowser(characterKey, app),
            AppKind.Code => CloseCode(characterKey, app),
            AppKind.Office => CloseOffice(characterKey, app),
            AppKind.Chat => CloseChat(characterKey, app),
            AppKind.Music => CloseMusic(characterKey, app),
            AppKind.Video => CloseVideo(characterKey, app),
            AppKind.Game => CloseGame(characterKey, app),
            AppKind.Note => CloseNote(characterKey, app),
            AppKind.Terminal => CloseTerminal(characterKey, app),
            AppKind.Utility => CloseUtility(characterKey, app),
            _ => CloseGeneric(characterKey, app)
        };
    }

    private static string CloseBrowser(string key, string app) => key switch
    {
        "miaobaibai" => $"{app}關掉了喵",
        "miaobubu" => $"不看{app}了？布布還想逛",
        "xiaotu" => $"{app}收起來，眼睛休息一下",
        "laowangmao" => $"{app}關掉，該賺的都看到了",
        "fengxiong" => $"{app}關了，別滑太晚",
        "fengge" => $"{app}已關閉。回來工作",
        "xiaoying" => $"{app}關了…可以專心一下了",
        "miaoniang" => $"不逛{app}了？喵娘還在",
        "tuge" => $"{app}關掉！下一件",
        "yamei" => $"{app}關掉了，眼睛會舒服一點",
        "yumei" => $"游出{app}了～",
        "gugugaga" => $"ググ，{app}關掉了",
        _ => $"你關掉了{app}"
    };

    private static string CloseCode(string key, string app) => key switch
    {
        "miaobaibai" => $"寫完了嗎？白白看你關了",
        "miaobubu" => $"不敲鍵盤了？布布鼓掌",
        "xiaotu" => $"{app}收工！去喝水",
        "laowangmao" => $"程式存好了，財運也存好",
        "fengxiong" => $"{app}關了，記得有存檔",
        "fengge" => $"{app}已結束。進度如何",
        "xiaoying" => $"終於關掉{app}…能下班了嗎",
        "miaoniang" => $"寫完要摸摸頭～",
        "tuge" => $"{app}收工！今天也有產出",
        "yamei" => $"不寫了就休息喔",
        "yumei" => $"這一波程式寫完了",
        "gugugaga" => $"ガガ，code 收起來了",
        _ => $"你關掉了{app}"
    };

    private static string CloseOffice(string key, string app) => key switch
    {
        "miaobaibai" => $"{app}收起來了，辛苦了",
        "miaobubu" => $"秘書布布宣布：{app}下班",
        "xiaotu" => $"{app}收工！文件存了嗎",
        "laowangmao" => $"{app}關了，業績入袋",
        "fengxiong" => $"{app}關了，今天也夠了",
        "fengge" => $"{app}已關閉。報告交了嗎",
        "xiaoying" => $"{app}關掉…這份總算結束了吧",
        "miaoniang" => $"{app}收好了，來休息嘛",
        "tuge" => $"{app}收工！效率讚",
        "yamei" => $"辦公告一段落了喔",
        "yumei" => $"文件退潮了，喘口氣",
        "gugugaga" => $"ググ，{app}下班了",
        _ => $"你關掉了{app}"
    };

    private static string CloseChat(string key, string app) => key switch
    {
        "miaobaibai" => $"{app}關掉了，等等再回也行",
        "miaobubu" => $"不聊天了？布布陪你",
        "xiaotu" => $"{app}靜音收工",
        "laowangmao" => $"貴人聊完了，繼續發財",
        "fengxiong" => $"{app}關了，少被訊息拉走",
        "fengge" => $"{app}已關閉。專注眼前",
        "xiaoying" => $"{app}關了，會議總該結束了吧",
        "miaoniang" => $"不回訊息了？那陪喵娘",
        "tuge" => $"{app}關掉，別再已讀不回了",
        "yamei" => $"聊天先停，好好休息",
        "yumei" => $"對岸暫時沒人叫你了",
        "gugugaga" => $"ガガ，{app}不說話了",
        _ => $"你關掉了{app}"
    };

    private static string CloseMusic(string key, string app) => key switch
    {
        "miaobaibai" => $"歌停了…有點安靜喵",
        "miaobubu" => $"音樂沒了，布布還想跳",
        "xiaotu" => $"BGM 停了，有點空",
        "laowangmao" => $"歌單收了，耳朵也招財完畢",
        "fengxiong" => $"音樂關了，世界突然很靜",
        "fengge" => $"{app}已停止。該開會了嗎",
        "xiaoying" => $"歌停了，加班比較沒力…",
        "miaoniang" => $"不唱歌了？喵娘有點寂寞",
        "tuge" => $"BGM 關掉，下一場！",
        "yamei" => $"音樂停了，靜靜也好",
        "yumei" => $"海浪聲也跟著停了",
        "gugugaga" => $"ググ…歌沒了",
        _ => $"你關掉了{app}"
    };

    private static string CloseVideo(string key, string app) => key switch
    {
        "miaobaibai" => $"影片看完了嗎？",
        "miaobubu" => $"不看了？布布還沒看夠",
        "xiaotu" => $"{app}關掉，眼睛歇一下",
        "laowangmao" => $"片看完，該回來發財了",
        "fengxiong" => $"{app}關了，記得眨眨眼",
        "fengge" => $"{app}已關閉。休息夠了就開工",
        "xiaoying" => $"偷閒結束…{app}關了",
        "miaoniang" => $"劇看完了？還想陪你",
        "tuge" => $"{app}關掉！別被片尾拖住",
        "yamei" => $"不看了就讓眼睛休息",
        "yumei" => $"螢幕裡的海退潮了",
        "gugugaga" => $"ガガ，片片結束了",
        _ => $"你關掉了{app}"
    };

    private static string CloseGame(string key, string app) => key switch
    {
        "miaobaibai" => $"不玩{app}了？下次再加油",
        "miaobubu" => $"遊戲關了，布布幫你慶功",
        "xiaotu" => $"{app}下線！今天有贏嗎",
        "laowangmao" => $"不玩了，爆金就算數",
        "fengxiong" => $"{app}關了，去睡覺比較實在",
        "fengge" => $"{app}已結束。正事接回來",
        "xiaoying" => $"{app}關了…明天上班還是會累",
        "miaoniang" => $"冒險結束，回喵娘這裡",
        "tuge" => $"{app}下了！下把再衝",
        "yamei" => $"不玩了就早點休息喔",
        "yumei" => $"冒險的人回到岸上了",
        "gugugaga" => $"ググ，冒險結束了",
        _ => $"你關掉了{app}"
    };

    private static string CloseNote(string key, string app) => key switch
    {
        "miaobaibai" => $"筆記本收好了喵",
        "miaobubu" => $"{app}闔上，貼紙也收好",
        "xiaotu" => $"重點寫完就關{app}",
        "laowangmao" => $"點子存起來，以後會值錢",
        "fengxiong" => $"{app}關了，寫過就好",
        "fengge" => $"待辦記下了就關",
        "xiaoying" => $"{app}關了，清單還是一樣長…",
        "miaoniang" => $"喵娘幫你記住了",
        "tuge" => $"{app}收！重點帶走",
        "yamei" => $"寫完就闔上囉",
        "yumei" => $"貝殼蓋起來了",
        "gugugaga" => $"ググ，字寫完了",
        _ => $"你關掉了{app}"
    };

    private static string CloseTerminal(string key, string app) => key switch
    {
        "miaobaibai" => $"黑視窗關掉了，白白比較安心",
        "miaobubu" => $"字字不見了，布布鬆口氣",
        "xiaotu" => $"駭客模式結束！",
        "laowangmao" => $"指令收工，財運別下錯檔",
        "fengxiong" => $"終端機關了，應該沒下錯吧",
        "fengge" => $"{app}已關閉。結果如何",
        "xiaoying" => $"黑畫面沒了…拜託有存到",
        "miaoniang" => $"咒語念完了嗎",
        "tuge" => $"終端機收工！希望是綠的",
        "yamei" => $"指令視窗關了喔",
        "yumei" => $"深海視窗合起來了",
        "gugugaga" => $"ガガ，黑窗口關掉了",
        _ => $"你關掉了{app}"
    };

    private static string CloseUtility(string key, string app) => key switch
    {
        "miaobaibai" => $"{app}用完囉",
        "miaobubu" => $"{app}收起來，布布看過了",
        "xiaotu" => $"{app}辦完就關",
        "laowangmao" => $"{app}用完，辦事順利",
        "fengxiong" => $"{app}關了，剛好",
        "fengge" => $"{app}已關閉。繼續下一件",
        "xiaoying" => $"雜事處理完了嗎，{app}關了",
        "miaoniang" => $"{app}收好了喵",
        "tuge" => $"{app}解決！下一件",
        "yamei" => $"{app}用完了喔",
        "yumei" => $"{app}漂走了",
        "gugugaga" => $"ググ，{app}不見了",
        _ => $"你關掉了{app}"
    };

    private static string CloseGeneric(string key, string app) => key switch
    {
        "miaobaibai" => $"你關掉了{app}喵",
        "miaobubu" => $"{app}掰掰！",
        "xiaotu" => $"{app}收起來了",
        "laowangmao" => $"{app}關了，萬事順心",
        "fengxiong" => $"看到你關掉{app}了",
        "fengge" => $"{app}已關閉",
        "xiaoying" => $"少一個視窗了…{app}",
        "miaoniang" => $"{app}走了？喵娘還在",
        "tuge" => $"{app}下場！",
        "yamei" => $"你關掉{app}了呢",
        "yumei" => $"{app}游走了",
        "gugugaga" => $"ガガ，{app}關掉了",
        _ => $"你關掉了{app}"
    };
}
