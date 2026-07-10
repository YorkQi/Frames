
Create DataBase Frames default character set utf8mb4 collate utf8mb4_unicode_ci;

use Frames;

DROP TABLE `User`;

CREATE TABLE `User`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(128)  NOT NULL COMMENT '用户名称',
  `Sex` int(1) NOT NULL COMMENT '性别',
  `ProfilePicture` varchar(128) NOT NULL COMMENT '头像',
  `CreateTime` bigint(20) NOT NULL COMMENT '创建时间',
  PRIMARY KEY (`Id`)
);