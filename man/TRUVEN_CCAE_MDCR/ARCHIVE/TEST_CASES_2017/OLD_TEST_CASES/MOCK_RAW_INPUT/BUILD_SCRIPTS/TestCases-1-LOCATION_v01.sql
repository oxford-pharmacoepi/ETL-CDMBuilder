/*LOCATION TABLE*/

/*101*/
INSERT [dbo].[ENROLLMENT_DETAIL] ([DOBYR], [DTEND], [DTSTART], [EGEOLOC], [ENROLID], [RX], [SEX]) 
VALUES (1970, CAST(N'2012-07-31' AS Date), CAST(N'2012-07-01' AS Date), N'11', 101, N'1', N'1')
/*102*/
INSERT [dbo].[ENROLLMENT_DETAIL] ([DOBYR], [DTEND], [DTSTART], [EGEOLOC], [ENROLID], [RX], [SEX]) 
VALUES (1970, CAST(N'2012-07-31' AS Date), CAST(N'2012-07-01' AS Date), N'89', 102, N'1', N'1')
/*103*/
INSERT [dbo].[ENROLLMENT_DETAIL] ([DOBYR], [DTEND], [DTSTART], [EGEOLOC], [ENROLID], [RX], [SEX]) 
VALUES (1970, CAST(N'2012-07-31' AS Date), CAST(N'2012-07-01' AS Date), N'38', 103, N'1', N'1')

INSERT [dbo].[ENROLLMENT_DETAIL] ([DOBYR], [DTEND], [DTSTART], [EGEOLOC], [ENROLID], [RX], [SEX]) 
VALUES (1970, CAST(N'2012-06-30' AS Date), CAST(N'2012-06-01' AS Date), N'38', 103, N'1',N'1')


--DELETE FROM ENROLLMENT_DETAIL WHERE ENROLID IN ('101','102','103')

SELECT * FROM ENROLLMENT_DETAIL WHERE ENROLID IN ('101','102','103')