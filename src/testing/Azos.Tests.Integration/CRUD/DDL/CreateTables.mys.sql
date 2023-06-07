﻿create schema `AzosTest`;

use `AzosTest`;


create table TBL_DOCTOR
(
  COUNTER   BIGINT  not null  AUTO_INCREMENT,
    FIRST_NAME  varchar(25) not null,
    LAST_NAME   varchar(25) not null,
    SSN         char(11) not null,
    NPI         char(25) not null,
    DOB      date not null,
    ADDRESS1    varchar(25),
    ADDRESS2    varchar(25),
    CITY        varchar(15),
    STATE       char(2),
    ZIP         char(10),
    PHONE       char(15),
    IS_CERTIFIED char(1),

    YEARS_IN_SERVICE   int,
    AMOUNT             decimal(12,4),
    NOTE         text,
    PRIMARY KEY (COUNTER),
    UNIQUE INDEX `SSN` (`SSN`)
);


create table TBL_PATIENT
(
  COUNTER   BIGINT  not null  AUTO_INCREMENT,
    FNAME  varchar(25) not null,
    LNAME   varchar(25) not null,
    SSN         char(11) not null,
    DOB      date not null,
    ADDRESS1    varchar(25),
    ADDRESS2    varchar(25),
    CITY        varchar(15),
    STATE       char(2),
    ZIP         char(10),
    PHONE       char(15),

    YEARS_IN_SERVICE   int,
    AMOUNT             decimal(12,4),
    NOTE         text,
    c_doctor           bigint references TBL_DOCTOR(COUNTER),
    PRIMARY KEY (COUNTER)
);

create table TBL_TYPES
(
  GDID     BIGINT UNSIGNED not null,

  SCREEN_NAME  char(32) not null,

  STRING_NAME   varchar(25) not null,
  CHAR_NAME    char(25) not null,

  BOOL_CHAR  char(1),
  BOOL_BOOL  bool,
  AMOUNT     DECIMAL(12,4),
  DOB   date,
  AGE   int,

  PRIMARY KEY (GDID)
);

create table TBL_FULLGDID
(
  GDID        BINARY(12) not null,
  VARGDID     VARBINARY(12) not null,
  STRING_NAME   varchar(25) not null,
  PRIMARY KEY (GDID)
);


create table TBL_TUPLE
(
  COUNTER   BIGINT  not null,
  DATA  varchar(25) not null,
  PRIMARY KEY (COUNTER)
);