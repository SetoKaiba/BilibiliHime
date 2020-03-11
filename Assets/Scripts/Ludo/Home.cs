using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CI.TaskParallel;
using SpeechLib;
using UnityEngine;
using Random = UnityEngine.Random;

public class Home : SingletonUtil<Home>
{
    public List<HomeCell> yellowHomeCells = new List<HomeCell>();
    public List<HomeCell> blueHomeCells = new List<HomeCell>();
    public List<HomeCell> greenHomeCells = new List<HomeCell>();
    public List<HomeCell> redHomeCells = new List<HomeCell>();

    public Cell yellowStart;
    public Cell blueStart;
    public Cell greenStart;
    public Cell redStart;

    public List<Piece> yellowControllablePiece = new List<Piece>();
    public List<Piece> blueControllablePiece = new List<Piece>();
    public List<Piece> greenControllablePiece = new List<Piece>();
    public List<Piece> redControllablePiece = new List<Piece>();

    public Piece yellowPiecePrefab;
    public Piece bluePiecePrefab;
    public Piece greenPiecePrefab;
    public Piece redPiecePrefab;

    public Piece.Team current;
    public int point;
    public State state;

    public int countDown;
    public int currentCountDown;
    public Dictionary<string, int> rollDict = new Dictionary<string, int>();

    public string redPlayer;
    public string yellowPlayer;
    public string bluePlayer;
    public string greenPlayer;

    public bool redWin;
    public bool yellowWin;
    public bool blueWin;
    public bool greenWin;

    public SpVoice spVoice;
    public float timeout;
    public float accTime;

    IEnumerator Start()
    {
        spVoice = new SpVoiceClass();
        spVoice.Voice = spVoice.GetVoices(string.Empty, string.Empty).Item(0);

        yield return null;
        Ready();
    }

    void SpeakPlayerList()
    {
        spVoice.Skip("Sentence", int.MaxValue);
        spVoice.Speak(PlayerList.Instance.text.text);
    }

    void SpeakInfo()
    {
        spVoice.Skip("Sentence", int.MaxValue);
        spVoice.Speak(Info.Instance.text.text, SpeechVoiceSpeakFlags.SVSFlagsAsync);
    }

    void Ready()
    {
        foreach (var piece in FindObjectsOfType<Piece>())
        {
            Destroy(piece.gameObject);
        }

        int i = 1;
        foreach (var homeCell in yellowHomeCells)
        {
            homeCell.piece = Instantiate(yellowPiecePrefab, homeCell.transform.position, Quaternion.identity);
            homeCell.piece.index = i;
            i++;
        }

        i = 1;
        foreach (var homeCell in blueHomeCells)
        {
            homeCell.piece = Instantiate(bluePiecePrefab, homeCell.transform.position, Quaternion.identity);
            homeCell.piece.index = i;
            i++;
        }

        i = 1;
        foreach (var homeCell in greenHomeCells)
        {
            homeCell.piece = Instantiate(greenPiecePrefab, homeCell.transform.position, Quaternion.identity);
            homeCell.piece.index = i;
            i++;
        }

        i = 1;
        foreach (var homeCell in redHomeCells)
        {
            homeCell.piece = Instantiate(redPiecePrefab, homeCell.transform.position, Quaternion.identity);
            homeCell.piece.index = i;
            i++;
        }

        state = State.Ready;
        BilibiliLiveNetty.Instance.onDanmakuMessage = (uname, message) =>
        {
            if (message.ToLower() == "roll")
            {
                UnityTask.RunOnUIThread(() =>
                {
                    if (!rollDict.ContainsKey(uname))
                    {
                        rollDict[uname] = Random.Range(0, 100);
                    }
                });
            }
        };

        currentCountDown = countDown;
        rollDict.Clear();
        PlayerList.Instance.text.text = "";
        Info.Instance.text.text = $"倒计时{currentCountDown}秒，请观众发送弹幕Roll进行Roll点，Roll点最高的4个将进入游戏";
        SpeakInfo();
        StartCoroutine(ReadyCoroutine());
    }

    IEnumerator ReadyCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            currentCountDown--;
            Info.Instance.text.text = $"倒计时{currentCountDown}秒，请观众发送弹幕Roll进行Roll点，Roll点最高的4个将进入游戏";

            var str = "";
            var players = rollDict.Keys.OrderByDescending(key => rollDict[key]).ToList();
            foreach (var player in players)
            {
                str += $"{player}:{rollDict[player]}\n";
            }

            PlayerList.Instance.text.text = str;

            if (currentCountDown != 0) continue;
            if (players.Count >= 4)
            {
                redPlayer = players[0];
                yellowPlayer = players[1];
                bluePlayer = players[2];
                greenPlayer = players[3];
                PlayerList.Instance.text.text =
                    $"红色玩家:　{redPlayer}\n黄色玩家: {yellowPlayer}\n蓝色玩家: {bluePlayer}\n绿色玩家: {greenPlayer}";
                SpeakPlayerList();

                StartGame();
            }
            else
            {
                Ready();
            }

            break;
        }
    }

    void StartGame()
    {
        BilibiliLiveNetty.Instance.onDanmakuMessage = (uname, message) =>
        {
            UnityTask.RunOnUIThread(() =>
            {
                switch (state)
                {
                    case State.Roll:
                        switch (current)
                        {
                            case Piece.Team.Red:
                                if (uname == redPlayer && message.ToLower() == "roll")
                                {
                                    Roll();
                                    accTime = 0;
                                }

                                break;
                            case Piece.Team.Yellow:
                                if (uname == yellowPlayer && message.ToLower() == "roll")
                                {
                                    Roll();
                                    accTime = 0;
                                }

                                break;
                            case Piece.Team.Blue:
                                if (uname == bluePlayer && message.ToLower() == "roll")
                                {
                                    Roll();
                                    accTime = 0;
                                }

                                break;
                            case Piece.Team.Green:
                                if (uname == greenPlayer && message.ToLower() == "roll")
                                {
                                    Roll();
                                    accTime = 0;
                                }

                                break;
                        }

                        break;
                    case State.Select:
                        switch (current)
                        {
                            case Piece.Team.Red:
                                if (uname == redPlayer)
                                {
                                    var flag = int.TryParse(message, out int index);
                                    if (flag)
                                    {
                                        Select(index);
                                        accTime = 0;
                                    }
                                }

                                break;
                            case Piece.Team.Yellow:
                                if (uname == yellowPlayer)
                                {
                                    var flag = int.TryParse(message, out int index);
                                    if (flag)
                                    {
                                        Select(index);
                                        accTime = 0;
                                    }
                                }

                                break;
                            case Piece.Team.Blue:
                                if (uname == bluePlayer)
                                {
                                    var flag = int.TryParse(message, out int index);
                                    if (flag)
                                    {
                                        Select(index);
                                        accTime = 0;
                                    }
                                }

                                break;
                            case Piece.Team.Green:
                                if (uname == greenPlayer)
                                {
                                    var flag = int.TryParse(message, out int index);
                                    if (flag)
                                    {
                                        Select(index);
                                        accTime = 0;
                                    }
                                }

                                break;
                        }

                        break;
                }
            });
        };
        current = Piece.Team.Red;
        state = State.Roll;
        accTime = 0;
        Info.Instance.text.text = $"请红色玩家{redPlayer}投掷骰子，发送弹幕Roll进行Roll点";
        SpeakInfo();
    }

    void OnGUI()
    {
        foreach (var piece in FindObjectsOfType<Piece>())
        {
            var screenPoint = Camera.main.WorldToScreenPoint(piece.transform.position);
            var guiStyle = new GUIStyle();
            guiStyle.fontSize = 20;
            guiStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(new Vector2(screenPoint.x, Screen.height - screenPoint.y), new Vector2(100, 50)),
                piece.index.ToString(), guiStyle);
        }
    }

    void Update()
    {
        if (state != State.Ready)
        {
            accTime += Time.deltaTime;

            redWin = redHomeCells.All(homeCell => !homeCell.piece);

            redWin = redWin && redControllablePiece.Count == 0;

            yellowWin = yellowHomeCells.All(homeCell => !homeCell.piece);

            yellowWin = yellowWin && yellowControllablePiece.Count == 0;

            blueWin = blueHomeCells.All(homeCell => !homeCell.piece);

            blueWin = blueWin && blueControllablePiece.Count == 0;

            greenWin = greenHomeCells.All(homeCell => !homeCell.piece);

            greenWin = greenWin && greenControllablePiece.Count == 0;
            string str = null;
            if (redWin || yellowWin || blueWin || greenWin || accTime > timeout)
            {
                state = State.Ready;
                currentCountDown = countDown;
                if (redWin)
                {
                    str = $"获胜的玩家是{redPlayer}, {currentCountDown}秒后重新开始";
                }

                if (yellowWin)
                {
                    str = $"获胜的玩家是{yellowPlayer}, {currentCountDown}秒后重新开始";
                }

                if (blueWin)
                {
                    str = $"获胜的玩家是{bluePlayer}, {currentCountDown}秒后重新开始";
                }

                if (greenWin)
                {
                    str = $"获胜的玩家是{greenPlayer}，游戏结束, {currentCountDown}秒后重新开始";
                }

                if (accTime > timeout)
                {
                    str = $"玩家{timeout}秒无响应，游戏结束, {currentCountDown}秒后重新开始";
                }

                Info.Instance.text.text = str;
                SpeakInfo();
                StartCoroutine(RestartCoroutine());
            }
        }
    }

    IEnumerator RestartCoroutine(bool win = true)
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            currentCountDown--;
            string str = null;
            if (redWin)
            {
                str = $"获胜的玩家是{redPlayer}, {currentCountDown}秒后重新开始";
            }

            if (yellowWin)
            {
                str = $"获胜的玩家是{yellowPlayer}, {currentCountDown}秒后重新开始";
            }

            if (blueWin)
            {
                str = $"获胜的玩家是{bluePlayer}, {currentCountDown}秒后重新开始";
            }

            if (greenWin)
            {
                str = $"获胜的玩家是{greenPlayer}, {currentCountDown}秒后重新开始";
            }

            if (!win)
            {
                str = $"玩家{timeout}秒无响应，游戏结束, {currentCountDown}秒后重新开始";
            }

            Info.Instance.text.text = str;

            if (currentCountDown == 0)
            {
                Ready();
                break;
            }
        }
    }

    public void Roll()
    {
        if (state != State.Roll)
            return;
        point = Random.Range(1, 7);

        string str = null;
        switch (current)
        {
            case Piece.Team.Yellow:
                if (point != 6 && yellowControllablePiece.Count == 0)
                {
                    str = $"Roll点为{point}，请蓝色玩家{bluePlayer}投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                    current = Piece.Team.Blue;
                }
                else
                {
                    str = $"Roll点为{point}，请黄色玩家{yellowPlayer}选择飞机或起飞: ";
                    state = State.Select;
                    if (point == 6 && yellowControllablePiece.Count < yellowHomeCells.Count)
                    {
                        str += "0-起飞,";
                    }

                    foreach (var piece in yellowControllablePiece)
                    {
                        str += piece.index + ",";
                    }
                }

                break;
            case Piece.Team.Blue:
                if (point != 6 && blueControllablePiece.Count == 0)
                {
                    str = $"Roll点为{point}，请绿色玩家{greenPlayer}投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                    current = Piece.Team.Green;
                }
                else
                {
                    str = $"Roll点为{point}，请蓝色玩家{bluePlayer}选择飞机或起飞: ";
                    state = State.Select;
                    if (point == 6 && blueControllablePiece.Count < blueHomeCells.Count)
                    {
                        str += "0-起飞,";
                    }

                    foreach (var piece in blueControllablePiece)
                    {
                        str += piece.index + ",";
                    }
                }

                break;
            case Piece.Team.Green:
                if (point != 6 && greenControllablePiece.Count == 0)
                {
                    str = $"Roll点为{point}，请红色玩家{redPlayer}投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                    current = Piece.Team.Red;
                }
                else
                {
                    str = $"Roll点为{point}，请绿色玩家{greenPlayer}选择飞机或起飞: ";
                    state = State.Select;
                    if (point == 6 && greenControllablePiece.Count < greenHomeCells.Count)
                    {
                        str += "0-起飞,";
                    }

                    foreach (var piece in greenControllablePiece)
                    {
                        str += piece.index + ",";
                    }
                }

                break;
            case Piece.Team.Red:
                if (point != 6 && redControllablePiece.Count == 0)
                {
                    str = $"Roll点为{point}，请黄色玩家{yellowPlayer}投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                    current = Piece.Team.Yellow;
                }
                else
                {
                    str = $"Roll点为{point}，请红色玩家{redPlayer}选择飞机或起飞: ";
                    state = State.Select;
                    if (point == 6 && redControllablePiece.Count < redHomeCells.Count)
                    {
                        str += "0-起飞,";
                    }

                    foreach (var piece in redControllablePiece)
                    {
                        str += piece.index + ",";
                    }
                }

                break;
        }

        Info.Instance.text.text = str;
        SpeakInfo();
    }

    public void Select(int index)
    {
        if (state != State.Select)
            return;

        string str = null;
        switch (current)
        {
            case Piece.Team.Yellow:
                if (index == 0 && point == 6 && yellowControllablePiece.Count < yellowHomeCells.Count)
                {
                    foreach (var homeCell in yellowHomeCells)
                    {
                        var piece = homeCell.piece;
                        if (piece)
                        {
                            piece.transform.position = yellowStart.transform.position;
                            piece.cell = yellowStart;
                            yellowControllablePiece.Add(homeCell.piece);
                            homeCell.piece = null;
                            break;
                        }
                    }

                    str = $"请黄色玩家{yellowPlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                }
                else
                {
                    foreach (var piece in yellowControllablePiece)
                    {
                        if (piece.index == index)
                        {
                            if (point != 6)
                            {
                                current = Piece.Team.Blue;
                                str = $"请蓝色玩家{bluePlayer}投掷骰子，发送弹幕Roll进行Roll点";
                            }
                            else
                            {
                                str = $"请黄色玩家{yellowPlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
            case Piece.Team.Blue:
                if (index == 0 && point == 6 && blueControllablePiece.Count < blueHomeCells.Count)
                {
                    foreach (var homeCell in blueHomeCells)
                    {
                        var piece = homeCell.piece;
                        if (piece)
                        {
                            piece.transform.position = blueStart.transform.position;
                            piece.cell = blueStart;
                            blueControllablePiece.Add(homeCell.piece);
                            homeCell.piece = null;
                            break;
                        }
                    }

                    str = $"请蓝色玩家{bluePlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                }
                else
                {
                    foreach (var piece in blueControllablePiece)
                    {
                        if (piece.index == index)
                        {
                            if (point != 6)
                            {
                                current = Piece.Team.Green;
                                str = $"请绿色玩家{greenPlayer}投掷骰子，发送弹幕Roll进行Roll点";
                            }
                            else
                            {
                                str = $"请蓝色玩家{bluePlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
            case Piece.Team.Green:
                if (index == 0 && point == 6 && greenControllablePiece.Count < greenHomeCells.Count)
                {
                    foreach (var homeCell in greenHomeCells)
                    {
                        var piece = homeCell.piece;
                        if (piece)
                        {
                            piece.transform.position = greenStart.transform.position;
                            piece.cell = greenStart;
                            greenControllablePiece.Add(homeCell.piece);
                            homeCell.piece = null;
                            break;
                        }
                    }

                    str = $"请绿色玩家{greenPlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                }
                else
                {
                    foreach (var piece in greenControllablePiece)
                    {
                        if (piece.index == index)
                        {
                            if (point != 6)
                            {
                                current = Piece.Team.Red;
                                str = $"请红色玩家{redPlayer}投掷骰子，发送弹幕Roll进行Roll点";
                            }
                            else
                            {
                                str = $"请绿色玩家{greenPlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
            case Piece.Team.Red:
                if (index == 0 && point == 6 && redControllablePiece.Count < redHomeCells.Count)
                {
                    foreach (var homeCell in redHomeCells)
                    {
                        var piece = homeCell.piece;
                        if (piece)
                        {
                            piece.transform.position = redStart.transform.position;
                            piece.cell = redStart;
                            redControllablePiece.Add(homeCell.piece);
                            homeCell.piece = null;
                            break;
                        }
                    }

                    str = $"请红色玩家{redPlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                    state = State.Roll;
                }
                else
                {
                    foreach (var piece in redControllablePiece)
                    {
                        if (piece.index == index)
                        {
                            if (point != 6)
                            {
                                current = Piece.Team.Yellow;
                                str = $"请黄色玩家{yellowPlayer}投掷骰子，发送弹幕Roll进行Roll点";
                            }
                            else
                            {
                                str = $"请红色玩家{redPlayer}继续投掷骰子，发送弹幕Roll进行Roll点";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
        }

        Info.Instance.text.text = str;
        SpeakInfo();
    }
}

public enum State
{
    Ready,
    Roll,
    Select,
    Wait
}