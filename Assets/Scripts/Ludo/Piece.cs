using System;
using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public Team team;
    public Cell cell;
    public int remaining;
    public bool isMoving;
    public float jumpAirLength;
    public float jumpLandLength;
    public bool inCoroutine;
    public int index;

    public Animator animator;
    public Transform trans;

    private Vector3 startPos;
    private float accTime;
    private State previous;

    public enum Team
    {
        None,
        Red,
        Yellow,
        Blue,
        Green
    }

    public void Move(int steps)
    {
        previous = Home.Instance.state;
        Home.Instance.state = State.Wait;
        remaining = steps;
    }

    void Start()
    {
        trans = transform;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isMoving)
        {
            Cell nextActualCell = null;
            var lastJump = false;
            if (remaining > 0)
            {
                if (!cell.isNonLastStepConditional)
                {
                    if (remaining > 1)
                    {
                        nextActualCell = cell.nextCell;
                    }
                    else
                    {
                        nextActualCell = team == cell.nextCell.conditionalTeam
                            ? (cell.nextCell.isNonLastStepConditional
                                ? cell.nextCell
                                : cell.nextCell.conditionalNextCell)
                            : cell.nextCell;
                        lastJump = true;
                    }
                }
                else
                {
                    nextActualCell = team == cell.conditionalTeam
                        ? cell.nonLastStepNextCell
                        : (team == cell.nextCell.conditionalTeam ? cell.nextCell.conditionalNextCell : cell.nextCell);
                    if (remaining == 1)
                    {
                        lastJump = true;
                    }
                }
            }

            if (nextActualCell)
            {
                accTime += Time.deltaTime;
                trans.LookAt(nextActualCell.transform.position);
                trans.position = Vector3.Lerp(startPos, nextActualCell.transform.position, accTime / jumpAirLength);
                if (Vector3.Distance(trans.position, nextActualCell.transform.position) < 1e-4)
                {
                    remaining--;
                    cell.Pieces.Remove(this);
                    cell = nextActualCell;
                    cell.Pieces.Add(this);
                    if (lastJump)
                    {
                        Home.Instance.state = previous;
                        for (int i = cell.Pieces.Count - 1; i >= 0; i--)
                        {
                            var piece = cell.Pieces[i];
                            if (piece.team != team)
                            {
                                piece.Eaten();
                            }
                        }
                    }

                    isMoving = false;
                }
            }
            else
            {
                Home.Instance.state = previous;
                Finish();
            }
        }
        else
        {
            if (remaining > 0 && !inCoroutine)
            {
                StartCoroutine(StepRemaining());
            }
        }
    }

    public void Finish()
    {
        cell.Pieces.Remove(this);
        switch (team)
        {
            case Team.Red:
                Home.Instance.redControllablePiece.Remove(this);
                break;
            case Team.Yellow:
                Home.Instance.yellowControllablePiece.Remove(this);
                break;
            case Team.Blue:
                Home.Instance.blueControllablePiece.Remove(this);
                break;
            case Team.Green:
                Home.Instance.greenControllablePiece.Remove(this);
                break;
        }

        Destroy(gameObject);
    }

    public void Eaten()
    {
        cell.Pieces.Remove(this);
        switch (team)
        {
            case Team.Red:
                Home.Instance.redControllablePiece.Remove(this);
                foreach (var homeCell in Home.Instance.redHomeCells)
                {
                    if (homeCell.piece) continue;
                    trans.position = homeCell.transform.position;
                    homeCell.piece = this;
                    break;
                }

                break;
            case Team.Yellow:
                Home.Instance.yellowControllablePiece.Remove(this);
                foreach (var homeCell in Home.Instance.yellowHomeCells)
                {
                    if (homeCell.piece) continue;
                    trans.position = homeCell.transform.position;
                    homeCell.piece = this;
                    break;
                }
                break;
            case Team.Blue:
                Home.Instance.blueControllablePiece.Remove(this);
                foreach (var homeCell in Home.Instance.blueHomeCells)
                {
                    if (homeCell.piece) continue;
                    trans.position = homeCell.transform.position;
                    homeCell.piece = this;
                    break;
                }
                break;
            case Team.Green:
                Home.Instance.greenControllablePiece.Remove(this);
                foreach (var homeCell in Home.Instance.greenHomeCells)
                {
                    if (homeCell.piece) continue;
                    trans.position = homeCell.transform.position;
                    homeCell.piece = this;
                    break;
                }
                break;
        }
    }

    IEnumerator StepRemaining()
    {
        inCoroutine = true;
        yield return new WaitForSeconds(jumpLandLength);
        isMoving = true;
        startPos = trans.position;
        animator.SetTrigger("Jump");
        accTime = 0;
        inCoroutine = false;
    }
}