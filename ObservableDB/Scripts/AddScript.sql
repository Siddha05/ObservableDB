with SrcRows as(
	select num from (values(1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) as s (num)
)
insert into tblData(data)
	select 'Data ' + convert(nvarchar, num)
	FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS num 
		  FROM SrcRows as _10
			cross join SrcRows as _100
			cross join SrcRows as _1000
			cross join SrcRows as _10000) as result;