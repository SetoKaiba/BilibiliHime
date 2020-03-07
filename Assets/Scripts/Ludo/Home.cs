using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CI.TaskParallel;
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
    public Dictionary<string, int> rollDict = new Dictionary<string, int>();

    public string redPlayer;
    public string yellowPlayer;
    public string bluePlayer;
    public string greenPlayer;

    void Start()
    {
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

        Ready();
    }

    void Ready()
    {
        state = State.Ready;
        BilibiliLiveNetty.Instance.onDanmakuMessage = (uname, message) =>
        {
            if (message == "Roll")
            {
                UnityTask.RunOnUIThread(() => { rollDict[uname] = Random.Range(0, 100); });
            }
        };
        countDown = 60;
        rollDict.Clear();
        PlayerList.Instance.text.text = "";
        StartCoroutine(ReadyCoroutine());
    }

    IEnumerator ReadyCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            countDown--;
            Info.Instance.text.text = $"倒计时{countDown}秒，请观众发送弹幕Roll进行Roll点，Roll点最高的4个将进入游戏";
            if (countDown == 0)
            {
                var players = rollDict.Keys.OrderByDescending(key => rollDict[key]).ToList();
                if (players.Count >= 4)
                {
                    redPlayer = players[0];
                    yellowPlayer = players[1];
                    bluePlayer = players[2];
                    greenPlayer = players[3];
                    PlayerList.Instance.text.text =
                        $"红色玩家:　{redPlayer}\n黄色玩家: {yellowPlayer}\n蓝色玩家: {bluePlayer}\n绿色玩家: {greenPlayer}";
                    StartGame();
                }
                else
                {
                    Ready();
                }

                break;
            }
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
                                if (uname == redPlayer && message == "Roll")
                                    Roll();
                                break;
                            case Piece.Team.Yellow:
                                if (uname == yellowPlayer && message == "Roll")
                                    Roll();
                                break;
                            case Piece.Team.Blue:
                                if (uname == bluePlayer && message == "Roll")
                                    Roll();
                                break;
                            case Piece.Team.Green:
                                if (uname == greenPlayer && message == "Roll")
                                    Roll();
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
        Info.Instance.text.text = $"请红色玩家{redPlayer}投掷骰子，发送弹幕Roll进行Roll点";
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
                    if (point == 6)
                    {
                        str += "0-起飞,";
                    }
                    else
                    {
                        foreach (var piece in yellowControllablePiece)
                        {
                            str += piece.index + ",";
                        }
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
                    if (point == 6)
                    {
                        str += "0-起飞,";
                    }
                    else
                    {
                        foreach (var piece in blueControllablePiece)
                        {
                            str += piece.index + ",";
                        }
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
                    if (point == 6)
                    {
                        str += "0-起飞,";
                    }
                    else
                    {
                        foreach (var piece in greenControllablePiece)
                        {
                            str += piece.index + ",";
                        }
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
                    if (point == 6)
                    {
                        str += "0-起飞,";
                    }
                    else
                    {
                        foreach (var piece in redControllablePiece)
                        {
                            str += piece.index + ",";
                        }
                    }
                }

                break;
        }

        Info.Instance.text.text = str;
    }

    public void Select(int index)
    {
        if (state != State.Select)
            return;
        switch (current)
        {
            case Piece.Team.Yellow:
                if (index == 0 && point == 6)
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

                    Info.Instance.text.text = $"请黄色玩家{yellowPlayer}继续投掷骰子";
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
                                Info.Instance.text.text = $"请蓝色玩家{bluePlayer}投掷骰子";
                            }
                            else
                            {
                                Info.Instance.text.text = $"请黄色玩家{yellowPlayer}继续投掷骰子";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
            case Piece.Team.Blue:
                if (index == 0 && point == 6)
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

                    Info.Instance.text.text = $"请蓝色玩家{bluePlayer}继续投掷骰子";
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
                                Info.Instance.text.text = $"请绿色玩家{greenPlayer}投掷骰子";
                            }
                            else
                            {
                                Info.Instance.text.text = $"请蓝色玩家{bluePlayer}继续投掷骰子";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
            case Piece.Team.Green:
                if (index == 0 && point == 6)
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

                    Info.Instance.text.text = $"请绿色玩家{greenPlayer}继续投掷骰子";
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
                                Info.Instance.text.text = $"请红色玩家{redPlayer}投掷骰子";
                            }
                            else
                            {
                                Info.Instance.text.text = $"请绿色玩家{greenPlayer}继续投掷骰子";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
            case Piece.Team.Red:
                if (index == 0 && point == 6)
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

                    Info.Instance.text.text = $"请红色玩家{redPlayer}继续投掷骰子";
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
                                Info.Instance.text.text = $"请黄色玩家{yellowPlayer}投掷骰子";
                            }
                            else
                            {
                                Info.Instance.text.text = $"请红色玩家{redPlayer}继续投掷骰子";
                            }

                            state = State.Roll;

                            piece.Move(point);
                            break;
                        }
                    }
                }

                break;
        }
    }
}

public enum State
{
    Ready,
    Roll,
    Select,
    Wait
}