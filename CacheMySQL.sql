CREATE TABLE `fiap`.`users` (
  `Id` INT NOT NULL,
  `Name` VARCHAR(45) NULL,
  `Email` VARCHAR(45) NULL,
  PRIMARY KEY (`Id`));

SELECT * FROM users;

INSERT INTO users(id, name, email)
VALUES(1, 'Pedro', 'pedro@email.com');