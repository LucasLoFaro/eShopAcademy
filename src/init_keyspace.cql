CREATE KEYSPACE products_keyspace WITH replication = { 'class' : 'SimpleStrategy', 'replication_factor' : 3 };

CREATE TABLE products_keyspace.products_by_category (
    Name TEXT,
    ProductId UUID,
    Price DECIMAL,
    Description TEXT,
    Image TEXT,
    CategoryDescription TEXT,
    PRIMARY KEY ((Name, ProductId), Price)
);

INSERT INTO products_keyspace.products_by_category (ProductId, Name, Price, Description, Image, CategoryDescription) 
VALUES (uuid(), 'Apples', 1.99, 'Fresh apples from local orchards', 'apples_image.jpg', 'Groceries');

INSERT INTO products_keyspace.products_by_category (ProductId, Name, Price, Description, Image, CategoryDescription) 
VALUES (uuid(), 'Bread', 2.49, 'Whole wheat bread for a healthy diet', 'bread_image.jpg', 'Groceries');

INSERT INTO products_keyspace.products_by_category (ProductId, Name, Price, Description, Image, CategoryDescription) 
VALUES (uuid(), 'Electric Guitar', 499.99, 'Professional electric guitar for musicians', 'guitar_image.jpg', 'Musical Instruments');

INSERT INTO products_keyspace.products_by_category (ProductId, Name, Price, Description, Image, CategoryDescription) 
VALUES (uuid(), 'Digital Piano', 799.99, 'High-quality digital piano with realistic sound', 'piano_image.jpg', 'Musical Instruments');

INSERT INTO products_keyspace.products_by_category (ProductId, Name, Price, Description, Image, CategoryDescription) 
VALUES (uuid(), 'Laptop', 999.99, 'Powerful laptop with high performance', 'laptop_image.jpg', 'Electronics');

INSERT INTO products_keyspace.products_by_category (ProductId, Name, Price, Description, Image, CategoryDescription) 
VALUES (uuid(), 'Smartphone', 699.99, 'Latest smartphone with advanced features', 'smartphone_image.jpg', 'Electronics');

CREATE TABLE products_keyspace.categories (
    CategoryId UUID,
    Name TEXT,
    Description TEXT,
    PRIMARY KEY ((Name, CategoryId), Description)
);

INSERT INTO products_keyspace.categories (CategoryId, Name, Description)
VALUES (uuid(), 'Groceries', 'Category for grocery items');

INSERT INTO products_keyspace.categories (CategoryId, Name, Description)
VALUES (uuid(), 'Musical Instruments', 'Category for musical instruments');

INSERT INTO products_keyspace.categories (CategoryId, Name, Description)
VALUES (uuid(), 'Electronics', 'Category for electronic devices');