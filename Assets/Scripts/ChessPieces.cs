using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None= 0, Pawn= 1, Bishop= 2, Rook= 3
}
public class ChessPieces : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPos;
    private Vector3 desiredScale = (Vector3.one)*100;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * 7);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 7);
    }

    public virtual List<Vector2Int> GetAvalibleMoves(ref ChessPieces[,] board, int tileCountX, int tilecountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        r.Add(new Vector2Int(2, 2));
        r.Add(new Vector2Int(3, 3));

        return r;
    }
    public virtual void SetPos(Vector3 position, bool force = false)
    {
        desiredPos = position;
        if (force)
            transform.position = desiredPos;
    }
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }

}
