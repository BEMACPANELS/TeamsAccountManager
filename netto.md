```mermaid
flowchart LR
  %% 幹線（4心）＝赤、パッチコード＝青（凡例だけ）
  classDef trunk stroke-width:2px,stroke:#cc3333,color:#cc3333;
  classDef patch stroke:#3366cc,color:#3366cc;

  %% 2F 新サーバ室
  subgraph A[2F – NEW SERVER ROOM]
    A_TB[Terminal Box\nODF 12 PORTS]
    A_SW[SW-2\n12 PORTS – 10G]
    A_TB -- Patch Cord --> A_SW
    class A_TB,A_SW patch
  end

  %% 2F 設計室
  subgraph B[2F – DESIGN ROOM]
    B_TB4[Terminal Box\nODF 4 PORTS]
    B_SW3[SW-3\n12 PORTS – 10G]
    B_TB8[Terminal Box\nODF 8 PORTS]
    B_SW4[SW-4\n12 PORTS – 10G]
    B_TB4 -- Patch Cord --> B_SW3
    B_TB8 -- Patch Cord --> B_SW4
    class B_TB4,B_SW3,B_TB8,B_SW4 patch
  end

  %% 1F 事務所
  subgraph C[1F – OFFICE AREA]
    C_IN[Existing Incoming Line]
    C_TB8[Terminal Box\nODF 8 PORTS]
    C_SW1[SW-1\n12 PORTS – 10G]
    C_IN --> C_TB8
    C_TB8 -- Patch Cord --> C_SW1
    class C_TB8,C_SW1 patch
  end

  %% 1F 工場（メッキ）
  subgraph D[1F – FACTORY AREA – MEKKI]
    D_TB8[Terminal Box\nODF 8 PORTS]
  end

  %% 1F 工場事務所
  subgraph E[1F – FACTORY OFFICE]
    E_TB4[Terminal Box\nODF 4 PORTS]
    E_SW5[SW-5\n12 PORTS – 10G]
    E_TB4 -- Patch Cord --> E_SW5
    class E_TB4,E_SW5 patch
  end

  %% 幹線4心（赤線で表現）
  C_TB8 == Fiber Optical Cable 4FO ==> B_TB8
  B_TB8 == Fiber Optical Cable 4FO ==> D_TB8
  D_TB8 == Fiber Optical Cable 4FO ==> E_TB4
  B_TB4 == Fiber Optical Cable 4FO ==> A_TB
  class C_TB8,B_TB8,D_TB8,E_TB4,B_TB4,A_TB trunk

  ```