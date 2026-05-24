namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI OFFSETS  ¡¤  Arena Breakout Infinite engine + game pointers
    //  Updated with latest offsets
    // /////////////////////////////////////////////////////////////////////////////////////

    public static class ABIOffsets
    {
        // //// GObjects / GNames / GWorld ///////////////////////////////////////////////
        public const ulong GWorld          = 0xA360908;
        public const ulong GNames          = 0xA881340;
        public const ulong GObjects        = 0xA5B6DF8;

        // FName decrypt function / key pointer naming kept for old code compatibility
        public const ulong DecryptKey      = 0xA8393DC;
        public const ulong FNameDecrypt    = 0xA8393DC;
        public const ulong GWorldDecrypt   = 0xA362908;

        // //// UObject //////////////////////////////////////////////////////////////////
        public const ulong UObject_ObjectId = 0x20;

        // //// UWorld ///////////////////////////////////////////////////////////////////
        public const ulong UWorld_PersistentLevel     = 0x30;
        public const ulong UWorld_NetDriver           = 0x38;
        public const ulong UWorld_GameState           = 0x120;
        public const ulong UWorld_Levels              = 0x138;
        public const ulong UWorld_OwningGameInstance  = 0x180;

        // //// UGameInstance ///////////////////////////////////////////////////////////
        public const ulong UGameInstance_LocalPlayers = 0x38;
        public const ulong UPlayer_PlayerController   = 0x30;

        // //// AController / APlayerController /////////////////////////////////////////
        public const ulong APlayerController_AcknowledgedPawn    = 0x348;
        public const ulong APlayerController_PlayerCameraManager = 0x3B0;

        // Kept old controller names for compatibility.
        // Verify these separately if your code uses them directly.
        public const ulong AController_ControlRotation = 0x378;
        public const ulong AController_Pawn            = 0x340;
        public const ulong AController_PlayerState     = 0x318;

        // //// AActor / APawn ///////////////////////////////////////////////////////////
        public const ulong AActor_RootComponent = 0x170;
        public const ulong AActor_PlayerState   = 0x348;
        public const ulong AActor_Owner         = 0x118;

        // //// ULevel //////////////////////////////////////////////////////////////////
        public const ulong ULevel_ActorArray = 0x98;
        public const ulong ULevel_ActorCount = 0xA0;

        // //// AGameStateBase //////////////////////////////////////////////////////////
        public const ulong AGameStateBase_PlayerArray = 0x330;
        public const ulong AGameStateBase_PlayerCount = 0x338;

        // //// ACharacter //////////////////////////////////////////////////////////////
        public const ulong ACharacter_Mesh = 0x388;

        // //// Scene / Skeletal Components /////////////////////////////////////////////
        public const ulong USceneComponent_AttachParent       = 0x108;
        public const ulong USceneComponent_RelativeLocation   = 0x16C;
        public const ulong USceneComponent_ComponentToWorld   = 0x210;
        public const ulong USceneComponent_ComponentToWorld_Ptr = 0x210;
        public const ulong USkeletalMeshComponent_ComponentToWorld = 0x210;

        // //// Camera //////////////////////////////////////////////////////////////////
        public const ulong APlayerCameraManager_CameraCachePrivate = 0x20E0;

        // Keep old field for compatibility; update if you verify it later.
        public const ulong APlayerCameraManager_LastFrameCameraCachePrivate = 0x27C0;

        // //// Skeletal Mesh Bones /////////////////////////////////////////////////////
        public const ulong USkeletalMeshComponent_BoneArray = 0x518;

        // Old names kept for compatibility with existing bone code.
        public const ulong USkeletalMeshComponent_CachedComponentSpaceTransforms = 0x518;
        public const ulong USkeletalMeshComponent_ComponentSpaceTransforms       = 0x518;

        // //// Network //////////////////////////////////////////////////////////////////
        public const ulong UNetDriver_ClientConnections = 0x90;
        public const ulong UNetConnection_OpenChannels  = 0x70;
        public const ulong UActorChannel_Actor          = 0x70;
        public const ulong UActorComponent_OwnerPrivate = 0x20;

        // //// ASGCharacter / UAM ///////////////////////////////////////////////////////
        public const ulong ASGCharacter_WeaponManager             = 0x1880;
        public const ulong ASGCharacter_AbilitySystemComponent    = 0x1740;
        public const ulong ASGCharacter_DeathComponent            = 0x1888;
        public const ulong ASGCharacter_CachedCharacterType       = 0x1645;
        public const ulong ASGCharacter_TeamId                    = 0x598;
        public const ulong ASGCharacter_WeaponEnemyArray          = 0xA98;
        public const ulong ASGCharacter_ArmorManager              = 0x1A18;
        public const ulong ASGCharacter_InventoryManager          = 0x19F0;

        // //// Ability / Death //////////////////////////////////////////////////////////
        public const ulong UAbilitySystemComponent_SpawnedAttributes = 0x188;
        public const ulong USGCharacterDeathComponent_bIsDead        = 0x240;

        // //// Mesh visibility timers ///////////////////////////////////////////////////
        public const ulong UPrimitiveComponent_LastSubmitTime = 0x2EC;
        public const ulong UPrimitiveComponent_LastRenderTime = 0x2F0;

        // //// Weapon Manager / Weapon //////////////////////////////////////////////////
        public const ulong USGCharacterWeaponManager_CurrentWeapon = 0x1F8;
        public const ulong USGCharacterWeaponManager_WeaponList    = 0x340;

        public const ulong ASGWeapon_WeaponZoomComponent = 0xB60;
        public const ulong ASGWeapon_WeaponAssembleComp  = 0xB48;

        public const ulong USGWeaponZoomComponent_ZoomProgressRate = 0x404;
        public const ulong USGWeaponZoomComponent_ScopeAdsScale    = 0x120;
        public const ulong USGWeaponZoomComponent_ScopeMagnification = 0x418;

        public const ulong USGWeaponAssembleComponent_CachedMagazine          = 0x280;
        public const ulong USGWeaponAssembleComponent_AssembledInventoryList  = 0x1F8;

        // //// Weapon Container / Ammo /////////////////////////////////////////////////
        public const ulong SGWeaponContainer              = 0x900;
        public const ulong USGWeaponContainer_MaxStack    = 0x120;
        public const ulong USGWeaponContainer_ContainList = 0x208;
        public const ulong FBulletContainerInfo_StackCount = 0x8;

        // //// Armor ///////////////////////////////////////////////////////////////////
        public const ulong USGCharacterArmorManagerComponent_ArmorList = 0x278;
        public const ulong ASGInventory_ArmorLevel                     = 0x6C8;
        public const ulong ASGInventory_ArmorCommonData                = 0x760;
        public const ulong USGInventoryCommonDataComponent_Durability  = 0x118;

        // //// Inventory ///////////////////////////////////////////////////////////////
        public const ulong USGCharacterInventoryManagerComponent_InventoryArray = 0x110;

        public const ulong ASGInventory_CommonDataComponent = 0x760;
        public const ulong ASGInventory_AssembleComp        = 0x7F8;

        public const ulong USGInventoryCommonDataComponent_StandardPrice      = 0x10C;
        public const ulong USGInventoryCommonDataComponent_ItemLocalizedName  = 0x170;

        // //// Old inventory names kept for compatibility //////////////////////////////
        public const ulong ASGInventory_ItemId        = 0x06A8; // not updated in provided list
        public const ulong ASGInventory_InventoryType = 0x0758; // not updated in provided list

        public const ulong CDC_TotalCount        = 0x0104;
        public const ulong CDC_StandardPrice     = 0x010C;
        public const ulong CDC_Rarity            = 0x0110;
        public const ulong CDC_SellPrice         = 0x0134;
        public const ulong CDC_DisplayName       = 0x0140;
        public const ulong CDC_SimpleDisplayName = 0x0170;

        public const ulong Container_SGInventoryCommonData = 0x08A8;
        public const ulong Container_ContainerMgr          = 0x08D8;

        public const ulong CMgr_InventoryContainerBaseList = 0x0140;
        public const ulong CMgr_IsRolledUp                 = 0x0150;
    }

    // /////////////////////////////////////////////////////////////////////////////////////
    //  EXTENDED OFFSETS  ¡¤  Vitals, death, weapon zoom
    // /////////////////////////////////////////////////////////////////////////////////////

    internal static class ABIOffsetsExt
    {
        // //// Pawn / Character /////////////////////////////////////////////////////////
        public const int OFF_PAWN_ASC              = 0x1740;
        public const int OFF_PAWN_DEATHCOMP        = 0x1888;
        public const int OFF_PAWN_WEAPONMAN        = 0x1880;
        public const int OFF_CHAR_TICKING_ON_DEATH = 0x1708; // not updated in provided list

        // //// AbilitySystemComponent //////////////////////////////////////////////////
        public const int OFF_ASC_ATTRSETS = 0x0188;

        // //// Health AttributeSet /////////////////////////////////////////////////////
        public const int OFF_ATTR_HEALTH    = 0x48;
        public const int OFF_ATTR_HEALTHMAX = 0x4C;

        // //// Death Component /////////////////////////////////////////////////////////
        public const int OFF_DEATHCOMP_DEATHINFO = 0x0240;
        public const int OFF_DEATHCOMP_BISDEAD   = 0x0240;

        // //// Mesh visibility timers //////////////////////////////////////////////////
        public const ulong OFF_MESH_TIMERS  = 0x2EC;
        public const int   OFF_LASTSUBMIT   = 0x0;
        public const int   OFF_LASTONSCREEN = 0x4;
        public const float VIS_TICK         = 0.06f;

        public const ulong OFF_LAST_SUBMIT_TIME = 0x2EC;
        public const ulong OFF_LAST_RENDER_TIME = 0x2F0;

        // //// Weapon Manager //////////////////////////////////////////////////////////
        public const ulong OFF_WEAPON_CURRENT = 0x1F8;

        // //// Weapon / Zoom ///////////////////////////////////////////////////////////
        public const ulong OFF_WEAPON_ZOOMCOMP   = 0xB60;
        public const ulong OFF_ZOOM_PROGRESSRATE = 0x404;
        public const ulong OFF_ZOOM_SCOPEMAG     = 0x418;
        public const ulong OFF_SCOPE_ADS_SCALE   = 0x120;

        // //// Weapon assemble / ammo //////////////////////////////////////////////////
        public const ulong OFF_WEAPON_ASSEMBLE_COMP = 0xB48;
        public const ulong OFF_CACHED_MAGAZINE      = 0x280;
        public const ulong OFF_SG_WEAPON_CONTAINER  = 0x900;
        public const ulong OFF_MAX_STACK            = 0x120;
        public const ulong OFF_CONTAIN_LIST         = 0x208;
        public const ulong OFF_STACK_COUNT          = 0x8;

        // //// Armor ///////////////////////////////////////////////////////////////////
        public const ulong OFF_CHARACTER_ARMOR_MANAGER = 0x1A18;
        public const ulong OFF_ARMOR_LIST              = 0x278;
        public const ulong OFF_ARMOR_LEVEL             = 0x6C8;
        public const ulong OFF_ARMOR_COMMON_DATA       = 0x760;
        public const ulong OFF_ARMOR_DURABILITY        = 0x118;

        // //// Inventory ///////////////////////////////////////////////////////////////
        public const ulong OFF_CHARACTER_INVENTORY_MANAGER = 0x19F0;
        public const ulong OFF_INVENTORY_ARRAY             = 0x110;
        public const ulong OFF_WEAPON_LIST                 = 0x340;
        public const ulong OFF_COMMON_DATA_COMPONENT       = 0x760;
        public const ulong OFF_ASSEMBLE_COMP               = 0x7F8;
        public const ulong OFF_STANDARD_PRICE              = 0x10C;
        public const ulong OFF_ITEM_LOCALIZED_NAME         = 0x170;
        public const ulong OFF_ASSEMBLED_INVENTORY_LIST    = 0x1F8;
    }
}