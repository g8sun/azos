﻿select GDID,
    REALM,
    G_USER,
    LEVEL_DOWN,
    ID,
    TID,
    PROVIDER,
    PWD,
    PROVIDER_DATA,
    START_UTC,
    END_UTC,
    PROPS,
    RIGHTS,
    CREATE_UTC,
    CREATE_ORIGIN,
    CREATE_ACTOR,
    LOCK_START_UTC,
    LOCK_END_UTC,
    LOCK_ACTOR,
    LOCK_NOTE,
    VERSION_UTC,
    VERSION_ORIGIN,
    VERSION_ACTOR,
    VERSION_STATE
from tbl_login TL
where
  (TL.G_USER = @g_user)
  AND (TL.REALM = @realm)