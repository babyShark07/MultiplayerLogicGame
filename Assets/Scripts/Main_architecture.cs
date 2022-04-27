using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class Main_architecture : MonoBehaviour
{
    
    [Header("Prefab && materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    [Header("Art and rendering")]
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float deathSize = 45f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 0.5f;

    //Logic of the actual board 
    private List<Vector2Int> avalibleMoves = new List<Vector2Int>();
    private ChessPieces currentlyDragging;
    private ChessPieces[,] chessPieces;
    private List<ChessPieces> deadWhites = new List<ChessPieces>();
    private List<ChessPieces> deadBlacks = new List<ChessPieces>();
    private const int TILE_COUNT_X = 6;
    private const int TILE_COUNT_Y = 6;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isBlackTurn;
    private bool isP1Turn;

    //Multiplayer Logic
    private int playerCount = -1;
    private int currentPlayer = -1;
    private bool localGame = true;
    public int CameraDirection = 0;

    private void Start()
    {
        isP1Turn = true;
        isBlackTurn = true;
        int is_P1turn = Convert.ToInt32(isP1Turn);
        GenerateallTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        //SpawnSinglePiece(ChessPieceType.Pawn, 0);
        PosAllPieces();
        RegisterEvents();
    }

    private void Update()
    {
        
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 200, LayerMask.GetMask("Tile", "Hover", "Test")))
        {
            //print(info.transform.name);
            //Get the index of the tile I've hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);  //change collider to transform

            //If we are hovering over a tile after not hovering any tiles
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //If we are already hovering over a tile, change the previous one 
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMoves(ref avalibleMoves, currentHover)) ? LayerMask.NameToLayer("Test") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //Down is pressed on mouse
            if(Input.GetMouseButtonDown(0))
            {
                if(chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    //is it our turn?
                    if(((chessPieces[hitPosition.x, hitPosition.y].team == 1 && isBlackTurn) ) || (chessPieces[hitPosition.x, hitPosition.y].team == 0 && !isBlackTurn ))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        avalibleMoves = currentlyDragging.GetAvalibleMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        HighlightTiles();
                    }
                }
            }
            if ( currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPos = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                if (ContainsValidMoves(ref avalibleMoves, new Vector2(hitPosition.x, hitPosition.y)))
                {
                    MoveTo(previousPos.x,previousPos.y , hitPosition.x, hitPosition.y);
                    //net imple
                    NetMakeMove nm = new NetMakeMove();
                    nm.originalX = previousPos.x;
                    nm.originalY = previousPos.y;
                    nm.destinationX = hitPosition.x;
                    nm.destinationY = hitPosition.y;
                    nm.playerId = currentPlayer;
                    Client.Instance.SendToServer(nm);
                }
                else
                {
                    currentlyDragging.SetPos(GetTileCenter(previousPos.x, previousPos.y));
                }
                currentlyDragging = null;
                RemovingHighlightTiles();
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMoves(ref avalibleMoves, currentHover)) ? LayerMask.NameToLayer("Test") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if(currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPos(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemovingHighlightTiles();
            }
        }

        //piece feedback hovering
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPos(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }  
    }



    //Generate the chess board
    private void GenerateallTiles(float tilesize, int tilecountX, int tilecountY)
    {
        yOffset = yOffset + transform.position.y;
        bounds = new Vector3((tilecountX / 2) * tileSize, 0, (tilecountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tilecountX, tilecountY];
        for (int x = 0; x < tilecountX; x++)
        {
            for (int y = 0; y < tilecountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tilesize, x, y); 
            }
        }
    }

    private GameObject GenerateSingleTile(float tilesize, int x, int y)
    {


        GameObject tileobject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileobject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileobject.AddComponent<MeshFilter>().mesh = mesh;
        tileobject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tilesize, yOffset, y * tilesize) - bounds;
        vertices[1] = new Vector3(x * tilesize, yOffset, (y+1) * tilesize) - bounds;
        vertices[2] = new Vector3((x+1) * tilesize, yOffset, y * tilesize) - bounds;
        vertices[3] = new Vector3((x+1)* tilesize, yOffset, (y+1) * tilesize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };
        mesh.vertices = vertices;
        mesh.triangles = tris;

        tileobject.layer = LayerMask.NameToLayer("Tile");

        mesh.RecalculateNormals();

        tileobject.AddComponent<BoxCollider>();

        return tileobject;
    }

    //Spawning of Pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPieces[TILE_COUNT_X, TILE_COUNT_Y];
        int whiteTeam = 0, blackTeam = 1;

        //White team spawnning
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[0, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        chessPieces[2, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        chessPieces[4, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        
        //Black Team Sapwnning 
        chessPieces[0, 5] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 5] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[2, 5] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[3, 5] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[4, 5] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[5, 5] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[0, 4] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        chessPieces[2, 4] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        chessPieces[4, 4] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);

    }
    private ChessPieces SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPieces cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPieces>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    //Positionong all the pieces
    private void PosAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if(chessPieces[x, y] != null)
                {
                    PosSinglePiece(x, y, true);
                }
            }
        }
    }

    private void PosSinglePiece (int x, int y, bool force= false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPos(GetTileCenter(x,y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //Higlight Tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < avalibleMoves.Count; i++)
            tiles[avalibleMoves[i].x, avalibleMoves[i].y].layer = LayerMask.NameToLayer("Test");
    }
    private void RemovingHighlightTiles()
    {
        for (int i = 0; i < avalibleMoves.Count; i++)
            tiles[avalibleMoves[i].x, avalibleMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        avalibleMoves.Clear();
    }

    //Operations
    private bool ContainsValidMoves(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;   
    }
    private void MoveTo(int originalX , int originalY, int x, int y)
    {
       
        ChessPieces cp = chessPieces[originalX, originalY];
        Vector2Int previousPos = new Vector2Int(originalX, originalY);
        
        //is there another piece ont eh smae position
        if(chessPieces[x,y] != null)
        {
            ChessPieces ccp = chessPieces[x, y];
            if(cp.team == ccp.team)
            {
                return;
            }
            
            if(ccp.team == 0)
            {
                deadWhites.Add(ccp);
                ccp.SetScale((Vector3.one) * deathSize);
                ccp.SetPos(new Vector3(6 * tileSize, yOffset, -1 * tileSize) - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2) +
                    (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                deadBlacks.Add(ccp);
                ccp.SetScale(Vector3.one * deathSize);
                ccp.SetPos(new Vector3(-1 * tileSize, yOffset, 6 * tileSize) - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2) +
                    (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPos.x, previousPos.y] = null;

        PosSinglePiece(x, y);

        if ((isP1Turn == true) && (isBlackTurn == true))
        {
            isBlackTurn = false;
            isP1Turn = true;
            currentPlayer = 0;
            
        }

        else if ((isP1Turn == true) && (isBlackTurn == false))
        {
            isBlackTurn = false;
            isP1Turn = false;
            currentPlayer = 1;
        }
        
        else if((isP1Turn == false) && (isBlackTurn == false))
        {
            isBlackTurn = true;
            isP1Turn = false;
            currentPlayer = 1;
        }
        
        else
        {
            isBlackTurn = true;
            isP1Turn = true;
            currentPlayer = 0;
        }


        if (localGame)
        {
            Game_ui.Instance.ChangeCamera(CameraAngle.whiteTeamOrPlayer1);

            if (currentPlayer == 0 && isBlackTurn)
            {
                currentPlayer = 0;
                Game_ui.Instance.ChangeCamera(CameraAngle.whiteTeamOrPlayer1);
            }

            else if (currentPlayer == 0 && !isBlackTurn)
            {
                currentPlayer = 1;
                Game_ui.Instance.ChangeCamera(CameraAngle.blackTeamOrPlayer2);

            }
            else if (currentPlayer == 1 && !isBlackTurn)
            {
                currentPlayer = 1;
                //Game_ui.Instance.ChangeCamera(CameraAngle.blackTeamOrPlayer2);
            }

            else
            {
                currentPlayer = 0;
                Game_ui.Instance.ChangeCamera(CameraAngle.whiteTeamOrPlayer1);
            }
        }

        //isBlackTurn = !isBlackTurn;

        return;
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one;
    }

    #region
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;

        Game_ui.Instance.SetLocalGame += OnSetLocalGame;
    }
    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;

        Game_ui.Instance.SetLocalGame -= OnSetLocalGame;

    }

    //Parsed by Server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        //Client has connected assign a team and return message back to him
        NetWelcome nw = msg as NetWelcome;

        //Assign the player
        nw.AssignedTeam = ++playerCount;

        //Return back to the client
        Server.Instance.SendToClient(cnn, nw);

        //if full start the game
        if(playerCount == 1)
            Server.Instance.Broadcast(new NetStartGame());
        

    }
    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetMakeMove nm = msg as NetMakeMove;
        //Recieve and just boradcast it back
        //Game_ui.Instance.ChangeCamera((CameraDirection == 0) ? CameraAngle.blackTeamOrPlayer2 : CameraAngle.whiteTeamOrPlayer1);
        Server.Instance.Broadcast(nm);
    }
    
    //Parsed by client
    private void OnWelcomeClient(NetMessage msg)
    {
        //Recieve the connection message
        NetWelcome nw = msg as NetWelcome;

        //Assign the player
        currentPlayer = nw.AssignedTeam;

        Debug.Log($"My assigned player is {nw.AssignedTeam}");

        if (localGame && currentPlayer == 0)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }

    }
    private void OnStartGameClient(NetMessage msg)
    {
        //We just need to change the camera
        Game_ui.Instance.ChangeCamera((currentPlayer == 0) ? CameraAngle.blackTeamOrPlayer2 : CameraAngle.whiteTeamOrPlayer1);
    }
    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove nm = msg as NetMakeMove;
        Debug.Log($"MM : {nm.playerId} : {nm.originalX} {nm.originalY} -> {nm.destinationX} {nm.destinationY} ");

        if(nm.playerId != currentPlayer || nm.playerId == currentPlayer)
        {
            MoveTo(nm.originalX, nm.originalY, nm.destinationX, nm.destinationY);
        }
        
    }
    private void OnSetLocalGame(bool v)
    {
        localGame = v;
    }

    #endregion
}
