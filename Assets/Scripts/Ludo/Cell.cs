using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public List<Piece> Pieces = new List<Piece>();
    
    public bool isNonLastStepConditional;
    public Cell nextCell;
    public Cell nonLastStepNextCell;
    
    public Cell conditionalNextCell;
    public Piece.Team conditionalTeam;
}
