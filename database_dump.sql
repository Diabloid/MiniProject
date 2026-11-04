-- MySQL dump 10.13  Distrib 8.0.43, for Win64 (x86_64)
--
-- Host: localhost    Database: miniproject_auth
-- ------------------------------------------------------
-- Server version	8.0.43

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Current Database: `miniproject_auth`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `miniproject_auth` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `miniproject_auth`;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20261015221344_InitialAuth','9.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `refreshtokens`
--

DROP TABLE IF EXISTS `refreshtokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `refreshtokens` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Token` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ExpiresAt` datetime(6) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `RevokedAt` datetime(6) DEFAULT NULL,
  `ReplacedByToken` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_RefreshTokens_Token` (`Token`),
  KEY `IX_RefreshTokens_UserId` (`UserId`),
  CONSTRAINT `FK_RefreshTokens_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `refreshtokens`
--

LOCK TABLES `refreshtokens` WRITE;
/*!40000 ALTER TABLE `refreshtokens` DISABLE KEYS */;
INSERT INTO `refreshtokens` VALUES (1,2,'8fLv5d/4Ga/eQkgQUkGRDB1aP39kw9WJrVhJ2zoGBC64+9aSrP8zVFSs54OqMo6t0X+ebMmoKdke6wgUdNkFyQ==','2026-10-27 23:04:31.368230','2026-10-20 23:04:31.368499',NULL,NULL),(2,2,'VmOQSBabD6o7xRGnweZdEyWzcj/9Jh8xED3HsoN8+jdITM2MHC5F2IUAvs3ZDk6xHCTHbg9EPA86spvQuLQ3lQ==','2026-10-27 23:07:15.783813','2026-10-20 23:07:15.783814',NULL,NULL),(3,2,'Bbb6f45dWNDIYiEA7LfYLTictYNY1DYxAsPYegEabaF/wQytz/oThXxJ3H7wa4IrOoD15F5JEMhucH4863zK2Q==','2026-10-27 23:07:53.990213','2026-10-20 23:07:53.990214',NULL,NULL),(4,2,'VIaAKxa/qnA+Q7C7keigHqb3v0q5CjWiWaZkOyywlsx9qfVWfozG3vHFrnNZSW9k/059XZJCWlaqy9cXFgHbMQ==','2026-10-27 23:10:07.123685','2026-10-20 23:10:07.123686',NULL,NULL),(5,1,'WSEDLMCJyANjZPJlxpC8qz35zLWNmHtMH+X49yszP9VbcxNQEHJIi3EQmg84UvPQ6/jzXV+0gSLoZcAZG0JOSw==','2026-10-27 23:22:40.009667','2026-10-20 23:22:40.009903','2026-10-20 23:25:57.085129','CbK2LpJyN0/7Ipb729gNpFl4EyNK/GzsaoA2wv2ynrKdFk6/MC7u1eWhXFv+Mg3aZImtrYhb6d45AiGDx+HR7A=='),(6,1,'CbK2LpJyN0/7Ipb729gNpFl4EyNK/GzsaoA2wv2ynrKdFk6/MC7u1eWhXFv+Mg3aZImtrYhb6d45AiGDx+HR7A==','2026-10-27 23:25:57.085540','2026-10-20 23:25:57.085541','2026-10-20 23:26:59.835519',NULL);
/*!40000 ALTER TABLE `refreshtokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'User'),(2,'Admin');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `userroles`
--

DROP TABLE IF EXISTS `userroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `userroles` (
  `UserId` int NOT NULL,
  `RoleId` int NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_UserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_UserRoles_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `roles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserRoles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `userroles`
--

LOCK TABLES `userroles` WRITE;
/*!40000 ALTER TABLE `userroles` DISABLE KEYS */;
INSERT INTO `userroles` VALUES (2,1),(1,2);
/*!40000 ALTER TABLE `userroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Email` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PasswordHash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Users_Email` (`Email`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin@novagames.com','$2a$11$ET0EfCtE2Xybgl/TjSjEaOfqWBf5F4pyAlUVLnDIlvPIro2dXKLe6','SuperAdmin','2026-01-01 00:00:00.000000',1),(2,'student@novagames.com','$2a$11$UgEQaEOQvQN.2HjhygTaXegbgSmaTOEFcmFxDOmFuFGR7fGtmrB3.','Student','2026-10-20 23:04:31.132164',1);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Current Database: `miniproject_catalog`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `miniproject_catalog` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `miniproject_catalog`;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20261020223644_InitialCatalog','9.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `categories`
--

DROP TABLE IF EXISTS `categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categories` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
INSERT INTO `categories` VALUES (1,'Action','Динамічні екшн-ігри'),(2,'RPG','Рольові ігри з глибоким сюжетом'),(3,'Strategy','Тактичні та стратегічні ігри'),(4,'Indie','Ігри від незалежних розробників');
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `items`
--

DROP TABLE IF EXISTS `items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `items` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Price` decimal(18,2) NOT NULL,
  `Discount` decimal(5,2) NOT NULL DEFAULT '0.00',
  `CategoryId` int NOT NULL,
  `ItemType` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CoverUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IconUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `GenresJson` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DownloadSize` double NOT NULL,
  `StockQuantity` int DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_Items_CategoryId` (`CategoryId`),
  KEY `IX_Items_IsActive_CategoryId` (`IsActive`,`CategoryId`),
  KEY `IX_Items_Title` (`Title`),
  CONSTRAINT `FK_Items_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `categories` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `items`
--

LOCK TABLES `items` WRITE;
/*!40000 ALTER TABLE `items` DISABLE KEYS */;
INSERT INTO `items` VALUES (1,'Cyberpunk 2077: Phantom Liberty','Пригодницький рольовий бойовик у відкритому світі темного майбутнього.',1199.00,15.00,2,'Digital','/Assets/default_game.png','/Assets/default_icon.png','[\"RPG\", \"Sci-Fi\", \"Action\"]',70.5,NULL,1,'2026-01-01 00:00:00.000000'),(2,'The Witcher 3: Wild Hunt - Complete Edition','Сюжетна рольова гра нового покоління у фантастичному відкритому всесвіті.',799.00,50.00,2,'Digital','/Assets/default_game.png','/Assets/default_icon.png','[\"RPG\", \"Open World\", \"Fantasy\"]',50,NULL,1,'2026-01-01 00:00:00.000000'),(3,'DOOM Eternal: Deluxe Edition','Армії пекла вторглися на Землю. Станьте Катом Року у битві за порятунок людства.',999.00,0.00,1,'Physical','/Assets/default_game.png','/Assets/default_icon.png','[\"Action\", \"Shooter\", \"First-Person\"]',0,25,1,'2026-01-01 00:00:00.000000'),(4,'Hollow Knight: Silksong','Досліджуйте розкішне королівство, де панують шовк та пісня.',450.00,0.00,4,'Digital','/Assets/default_game.png','/Assets/default_icon.png','[\"Indie\", \"Metroidvania\", \"Souls-like\"]',8.5,NULL,1,'2026-01-01 00:00:00.000000'),(5,'Grand Theft Auto VI','Нова частина культової серії у штаті Леоніда.',1999.00,0.00,1,'Digital',NULL,NULL,'[\"Action\",\"Open World\"]',1,NULL,1,'2026-10-20 23:23:47.573665');
/*!40000 ALTER TABLE `items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Current Database: `miniproject_ordering`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `miniproject_ordering` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `miniproject_ordering`;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20261102185500_InitialOrdering','9.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orderitems`
--

DROP TABLE IF EXISTS `orderitems`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orderitems` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `OrderId` int NOT NULL,
  `GameId` int NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UnitPrice` decimal(18,2) NOT NULL,
  `Quantity` int NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_OrderItems_GameId` (`GameId`),
  KEY `IX_OrderItems_OrderId` (`OrderId`),
  CONSTRAINT `FK_OrderItems_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=100 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orderitems`
--

LOCK TABLES `orderitems` WRITE;
/*!40000 ALTER TABLE `orderitems` DISABLE KEYS */;
INSERT INTO `orderitems` VALUES (1,1,1,'Cyberpunk 2077: Phantom Liberty',959.20,1),(2,2,8,'Tomb Raider: Definitive Edition',199.50,1),(3,3,10,'?????? #10',250.00,1),(4,4,9,'Sword Art Online: Alicization Lycoris',899.00,1),(5,5,3,'The Witcher 3: Wild Hunt',359.60,1),(6,6,19,'The Arbitrator',149.00,1),(7,7,7,'Mass Effect Legendary Edition',1499.00,1),(8,8,3,'The Witcher 3: Wild Hunt',449.50,1),(9,9,19,'The Arbitrator',149.00,1),(10,10,12,'Артбук \"The Art of Mass Effect Universe\"',1350.00,1),(11,11,3,'The Witcher 3: Wild Hunt',449.50,1);
/*!40000 ALTER TABLE `orderitems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orders`
--

DROP TABLE IF EXISTS `orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orders` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Status` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TotalAmount` decimal(18,2) NOT NULL,
  `PaymentMethod` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CustomerNotes` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IdempotencyKey` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Orders_IdempotencyKey` (`IdempotencyKey`),
  KEY `IX_Orders_Status` (`Status`),
  KEY `IX_Orders_UserId` (`UserId`)
) ENGINE=InnoDB AUTO_INCREMENT=100 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orders`
--

LOCK TABLES `orders` WRITE;
/*!40000 ALTER TABLE `orders` DISABLE KEYS */;
INSERT INTO `orders` VALUES (1,1,'Completed',959.20,'Card','Термінова доставка ліцензійного ключа','7b9f3a1e-842c-4c2e-9d21-f8a12c3e4567','2026-11-02 18:00:00.000000',NULL),(2,4,'Completed',199.50,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-2','2026-04-28 12:30:32.630420',NULL),(3,4,'Completed',250.00,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-3','2026-04-28 16:34:43.168104',NULL),(4,4,'Completed',899.00,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-4','2026-05-13 17:15:16.887860',NULL),(5,4,'Completed',359.60,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-5','2026-06-13 10:52:15.114362',NULL),(6,4,'Completed',149.00,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-6','2026-06-13 16:33:13.028531',NULL),(7,4,'Completed',1499.00,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-7','2026-06-16 19:24:00.746648',NULL),(8,8,'Pending',449.50,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-8','2026-06-16 19:32:18.320340',NULL),(9,8,'Completed',149.00,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-9','2026-06-16 19:32:48.270555',NULL),(10,4,'Pending',1350.00,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-10','2026-08-18 12:29:08.570341',NULL),(11,5,'Pending',449.50,'Card','?????????????????? ?? ???????????????????? ????','legacy-order-11','2026-08-19 15:02:38.453448',NULL);
/*!40000 ALTER TABLE `orders` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-11-02 23:36:04
