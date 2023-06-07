﻿delimiter ;.

INSERT INTO `tbl_user`
(`GDID`,
`GUID`,
`REALM`,
`NAME`,
`LEVEL`,
`DESCRIPTION`,
`START_UTC`,
`END_UTC`,
`ORG_UNIT`,
`PROPS`,
`RIGHTS`,
`NOTE`,
`CREATE_UTC`,
`CREATE_ORIGIN`,
`CREATE_ACTOR`,
`LOCK_START_UTC`,
`LOCK_END_UTC`,
`LOCK_ACTOR`,
`LOCK_NOTE`,
`VERSION_UTC`,
`VERSION_ORIGIN`,
`VERSION_ACTOR`,
`VERSION_STATE`)
VALUES
(
0x000000000000000000000001,
0xd1433bf1ed3c425ca9ad82a0e9e75512,
6906983, -- REALM hex version
'root', -- NAME
'S', -- LEVEL system
'Root system account for GDI realm', -- Description
'1000-01-01 00:00:00', -- START_UTC
'2100-01-01 00:00:00', -- END_UTC
null, -- ORG_UNIT
'{ p: { } }', -- PROPS
null, -- RIGHTS
'DO NOT REMOVE', -- NOTE
utc_timestamp(), -- CREATE_UTC
7567731,          -- CREATE_ORIGIN - 0x737973 = `sys`
'usrn@idp::root', -- CREATE_ACTOR
null, -- LOCK_START_UTC
null, -- LOCK_END_UTC
null, -- LOCK_ACTOR
null, -- LOCK_NOTE
'1000-01-01 00:00:00', -- VERSION_UTC
7567731,          -- VERSION_ORIGIN - 0x737973 = `sys`
'usrn@idp::root', -- VERSION_ACTOR
'C' -- VERSION_STATE
);.



delimiter ;.

INSERT INTO `tbl_login`
(`GDID`,
`REALM`,
`G_USER`,
`LEVEL_DOWN`,
`ID`,
`TID`,
`PROVIDER`,
`PWD`,
`PROVIDER_DATA`,
`START_UTC`,
`END_UTC`,
`PROPS`,
`RIGHTS`,
`CREATE_UTC`,
`CREATE_ORIGIN`,
`CREATE_ACTOR`,
`LOCK_START_UTC`,
`LOCK_END_UTC`,
`LOCK_ACTOR`,
`LOCK_NOTE`,
`VERSION_UTC`,
`VERSION_ORIGIN`,
`VERSION_ACTOR`,
`VERSION_STATE`)
VALUES
(
0x000000000000000000000001, -- GDID
6906983, -- GDI realm hex version
0x000000000000000000000001, -- G_USER
null, -- LEVEL_DOWN
'root', -- ID
25705, -- "ID" TID
7956003944985229683, -- syslogin
'{"alg":"KDF","fam":"Text","h":"8kinx_bcL0xz9q0viOe0Ro0Gcly7WXJtjT117cplLRw","s":"SjsLUw1-d_7ya-YR12atOm0Tr0Sjezg1XhIRv9dNR9o"}', -- thejake
null, --  PROVIDER_DATA
'1000-01-01 00:00:00', -- START_UTC
'2100-01-01 00:00:00', -- END_UTC
null, -- PROPS
null, -- RIGHTS
utc_timestamp(), -- CREATE_UTC
7567731,          -- CREATE_ORIGIN - 0x737973 = `sys`
'usrn@idp::root', -- CREATE_ACTOR
null, -- LOCK_START_UTC
null, -- LOCK_END_UTC
null, -- LOCK_ACTOR
null, -- LOCK_NOTE
'1000-01-01 00:00:00', -- VERSION_UTC
7567731,          -- VERSION_ORIGIN - 0x737973 = `sys`
'usrn@idp::root', -- VERSION_ACTOR
'C'
);.
