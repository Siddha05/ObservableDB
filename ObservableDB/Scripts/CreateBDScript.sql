use master;
go
if exists (select 1 from sys.databases where name = 'NotifyDB')
begin
	print 'Delete existing database'
	alter database NotifyDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
	drop database NotifyDB
end
go 

create database NotifyDB
on
(Name = 'NotifyDB',
filename = 'd:\TestDB\NotifyDB.mdf',
size = 30,
maxsize = 400,
filegrowth = 5)
log on
(Name = 'PLSE_NewLog',
filename = 'd:\TestDB\NotifyDB.ldf',
size = 30,
maxsize = 400,
filegrowth = 5
)

go

use NotifyDB;
go

alter database NotifyDB set enable_broker;

go
create table tblData(
id int identity(1,1) not null
	primary key,
flag bit not null
	default 1,
[data] nvarchar(max) not null -- тип text удалят в следующих версиях SQLSERVER
);