﻿-- future:      select -- todo: look at index optimization /+ INDEX(TG GSH)/
-- future:       distinct TL.G_NODE
-- future:      from
-- future:        tbl_geohash TG inner join tbl_nodelog TL on TG.G_NLOG = TL.GDID
-- future:      where
-- future:        (TG.GSH LIKE @GSH) AND
-- future:        (TL.START_UTC <= @asof) AND
-- future:        (TL.VERSION_STATE <> 'D')
-- future:      limit 8
