using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetradeBE.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banner",
                columns: table => new
                {
                    banner_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    redirect_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("banner_pkey", x => x.banner_id);
                });

            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("category_pkey", x => x.category_id);
                    table.ForeignKey(
                        name: "fk_category_parent",
                        column: x => x.parent_id,
                        principalTable: "category",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "image",
                columns: table => new
                {
                    image_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    alt_text = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("image_pkey", x => x.image_id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", maxLength: 100, nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("role_pkey", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "service_subscription",
                columns: table => new
                {
                    service_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    target_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: true),
                    benefits_description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("service_subscription_pkey", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    flag_count = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("User_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "attributes",
                columns: table => new
                {
                    attribute_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("attributes_pkey", x => x.attribute_id);
                    table.ForeignKey(
                        name: "fk_attr_category",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "category_id");
                });

            migrationBuilder.CreateTable(
                name: "category_image",
                columns: table => new
                {
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    image_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("category_image_pkey", x => new { x.category_id, x.image_id });
                    table.ForeignKey(
                        name: "fk_ci_category",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "fk_ci_image",
                        column: x => x.image_id,
                        principalTable: "image",
                        principalColumn: "image_id");
                });

            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    provider_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    is_password_set = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_pkey", x => x.account_id);
                    table.ForeignKey(
                        name: "fk_account_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "address",
                columns: table => new
                {
                    address_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receiver_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receiver_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    street = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    province_id = table.Column<int>(type: "integer", nullable: true),
                    district_id = table.Column<int>(type: "integer", nullable: true),
                    ward_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("address_pkey", x => x.address_id);
                    table.ForeignKey(
                        name: "fk_address_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "my_service",
                columns: table => new
                {
                    user_sub_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    service_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("my_service_pkey", x => x.user_sub_id);
                    table.ForeignKey(
                        name: "fk_ms_service",
                        column: x => x.service_id,
                        principalTable: "service_subscription",
                        principalColumn: "service_id");
                    table.ForeignKey(
                        name: "fk_ms_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    notification_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notification_pkey", x => x.notification_id);
                    table.ForeignKey(
                        name: "fk_notify_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    seller_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    stock_quantity = table.Column<int>(type: "integer", nullable: true),
                    weight_gram = table.Column<int>(type: "integer", nullable: true),
                    length_cm = table.Column<int>(type: "integer", nullable: true),
                    width_cm = table.Column<int>(type: "integer", nullable: true),
                    height_cm = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_pkey", x => x.product_id);
                    table.ForeignKey(
                        name: "fk_product_category",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "fk_product_seller",
                        column: x => x.seller_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_favorite",
                columns: table => new
                {
                    favorite_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_favorite_pkey", x => x.favorite_id);
                    table.ForeignKey(
                        name: "fk_ufav_category",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "fk_ufav_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_follow",
                columns: table => new
                {
                    follow_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    follower_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    followed_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_follow_pkey", x => x.follow_id);
                    table.ForeignKey(
                        name: "fk_uf_followed",
                        column: x => x.followed_user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_uf_follower",
                        column: x => x.follower_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_report",
                columns: table => new
                {
                    report_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reporter_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_report_pkey", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_ur_reporter",
                        column: x => x.reporter_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_ur_reviewer",
                        column: x => x.reviewed_by,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "user_search",
                columns: table => new
                {
                    search_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    keyword = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_search_pkey", x => x.search_id);
                    table.ForeignKey(
                        name: "fk_us_category",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "fk_us_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "voucher",
                columns: table => new
                {
                    voucher_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    seller_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    min_order_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    expiration_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("voucher_pkey", x => x.voucher_id);
                    table.ForeignKey(
                        name: "fk_voucher_seller",
                        column: x => x.seller_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "wishlist",
                columns: table => new
                {
                    wishlist_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("wishlist_pkey", x => x.wishlist_id);
                    table.ForeignKey(
                        name: "fk_wishlist_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "account_role",
                columns: table => new
                {
                    account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_role_pkey", x => new { x.account_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_ar_account",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_ar_role",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "role_id");
                });

            migrationBuilder.CreateTable(
                name: "auction",
                columns: table => new
                {
                    auction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    seller_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    starting_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    current_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    min_increment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    reserve_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    buy_now_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    winner_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("auction_pkey", x => x.auction_id);
                    table.ForeignKey(
                        name: "fk_auction_product",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "fk_auction_seller",
                        column: x => x.seller_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_auction_winner",
                        column: x => x.winner_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "chat_room",
                columns: table => new
                {
                    room_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    buyer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    seller_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_room_pkey", x => x.room_id);
                    table.ForeignKey(
                        name: "fk_cr_buyer",
                        column: x => x.buyer_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_cr_product",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "fk_cr_seller",
                        column: x => x.seller_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "offer",
                columns: table => new
                {
                    offer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    buyer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    offer_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("offer_pkey", x => x.offer_id);
                    table.ForeignKey(
                        name: "fk_offer_buyer",
                        column: x => x.buyer_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_offer_product",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "product_attribute",
                columns: table => new
                {
                    product_attribute_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    attribute_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    value = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_attribute_pkey", x => x.product_attribute_id);
                    table.ForeignKey(
                        name: "fk_pa_attr",
                        column: x => x.attribute_id,
                        principalTable: "attributes",
                        principalColumn: "attribute_id");
                    table.ForeignKey(
                        name: "fk_pa_product",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "product_image",
                columns: table => new
                {
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    image_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_main = table.Column<bool>(type: "boolean", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_image_pkey", x => new { x.product_id, x.image_id });
                    table.ForeignKey(
                        name: "fk_pi_image",
                        column: x => x.image_id,
                        principalTable: "image",
                        principalColumn: "image_id");
                    table.ForeignKey(
                        name: "fk_pi_product",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "my_voucher",
                columns: table => new
                {
                    user_voucher_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    voucher_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    used_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("my_voucher_pkey", x => x.user_voucher_id);
                    table.ForeignKey(
                        name: "fk_mv_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_mv_voucher",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "voucher_id");
                });

            migrationBuilder.CreateTable(
                name: "wishlist_item",
                columns: table => new
                {
                    wishlist_item_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    wishlist_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("wishlist_item_pkey", x => x.wishlist_item_id);
                    table.ForeignKey(
                        name: "fk_wi_product",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "fk_wi_wishlist",
                        column: x => x.wishlist_id,
                        principalTable: "wishlist",
                        principalColumn: "wishlist_id");
                });

            migrationBuilder.CreateTable(
                name: "auction_deposit",
                columns: table => new
                {
                    auction_deposit_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    auction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    policy_accepted = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("auction_deposit_pkey", x => x.auction_deposit_id);
                    table.ForeignKey(
                        name: "fk_ad_auction",
                        column: x => x.auction_id,
                        principalTable: "auction",
                        principalColumn: "auction_id");
                    table.ForeignKey(
                        name: "fk_ad_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "bid",
                columns: table => new
                {
                    bid_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    auction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("bid_pkey", x => x.bid_id);
                    table.ForeignKey(
                        name: "fk_bid_auction",
                        column: x => x.auction_id,
                        principalTable: "auction",
                        principalColumn: "auction_id");
                    table.ForeignKey(
                        name: "fk_bid_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "chat",
                columns: table => new
                {
                    chat_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    room_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sender_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    message_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_pkey", x => x.chat_id);
                    table.ForeignKey(
                        name: "fk_chat_room",
                        column: x => x.room_id,
                        principalTable: "chat_room",
                        principalColumn: "room_id");
                    table.ForeignKey(
                        name: "fk_chat_sender",
                        column: x => x.sender_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    seller_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    voucher_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_snapshot = table.Column<string>(type: "text", nullable: true),
                    auction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    offer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tracking_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipping_provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    shipping_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    final_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    expected_delivery_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Order_pkey", x => x.order_id);
                    table.ForeignKey(
                        name: "fk_order_auction",
                        column: x => x.auction_id,
                        principalTable: "auction",
                        principalColumn: "auction_id");
                    table.ForeignKey(
                        name: "fk_order_offer",
                        column: x => x.offer_id,
                        principalTable: "offer",
                        principalColumn: "offer_id");
                    table.ForeignKey(
                        name: "fk_order_product",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "product_id");
                    table.ForeignKey(
                        name: "fk_order_seller",
                        column: x => x.seller_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_order_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_order_voucher",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "voucher_id");
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    provider_transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_pkey", x => x.payment_id);
                    table.ForeignKey(
                        name: "fk_payment_order",
                        column: x => x.order_id,
                        principalTable: "Order",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "fk_payment_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "refund_request",
                columns: table => new
                {
                    refund_request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_account_holder = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("refund_request_pkey", x => x.refund_request_id);
                    table.ForeignKey(
                        name: "fk_rr_order",
                        column: x => x.order_id,
                        principalTable: "Order",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "fk_rr_user",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "review",
                columns: table => new
                {
                    review_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reviewer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    seller_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    order_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("review_pkey", x => x.review_id);
                    table.ForeignKey(
                        name: "fk_review_order",
                        column: x => x.order_id,
                        principalTable: "Order",
                        principalColumn: "order_id");
                    table.ForeignKey(
                        name: "fk_review_reviewer",
                        column: x => x.reviewer_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "fk_review_seller",
                        column: x => x.seller_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "user_id", "avatar_url", "created_at", "email", "first_name", "is_deleted", "last_name", "phone", "updated_at" },
                values: new object[,]
                {
                    { "USER_ADMIN", null, null, "admin@retrade.com", "Admin", null, "System", null, null },
                    { "USER_BUYER", null, null, "buyer@retrade.com", "Demo", null, "Buyer", null, null },
                    { "USER_SELLER", null, null, "seller@retrade.com", "Demo", null, "Seller", null, null }
                });

            migrationBuilder.InsertData(
                table: "role",
                columns: new[] { "role_id", "name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Buyer" },
                    { 3, "Seller" }
                });

            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "account_id", "created_at", "is_deleted", "last_login_at", "password_hash", "provider", "provider_user_id", "status", "updated_at", "user_id", "username" },
                values: new object[,]
                {
                    { "ACC_ADMIN", null, null, null, "$2a$11$XFDcgQtapRKlxhrDeIKT.ONkYyy2rdIr4KuhAA227mymHjQcwvARK", "LOCAL", null, "Active", null, "USER_ADMIN", "admin" },
                    { "ACC_BUYER", null, null, null, "$2a$11$skXVMZHvw/ATfZrHOWofzueSzR613nON14UP.Oebr3pqDIg4pb7pu", "LOCAL", null, "Active", null, "USER_BUYER", "buyer" },
                    { "ACC_SELLER", null, null, null, "$2a$11$eq3/BuW/5icBnDHOYjfO1eYOG1SVa6YEQp/oZQPtXV2RHj7sCZi8G", "LOCAL", null, "Active", null, "USER_SELLER", "seller" }
                });

            migrationBuilder.InsertData(
                table: "account_role",
                columns: new[] { "account_id", "role_id", "created_at" },
                values: new object[,]
                {
                    { "ACC_ADMIN", 1, null },
                    { "ACC_BUYER", 2, null },
                    { "ACC_SELLER", 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_user_id",
                table: "account",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_role_role_id",
                table: "account_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_address_user_id",
                table: "address",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_attributes_category_id",
                table: "attributes",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_product_id",
                table: "auction",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_seller_id",
                table: "auction",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_winner_id",
                table: "auction",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposit_auction_id",
                table: "auction_deposit",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "IX_auction_deposit_user_id",
                table: "auction_deposit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bid_auction_id",
                table: "bid",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "IX_bid_user_id",
                table: "bid",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_parent_id",
                table: "category",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_image_image_id",
                table: "category_image",
                column: "image_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_id",
                table: "chat",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_sender_id",
                table: "chat",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_buyer_id",
                table: "chat_room",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_product_id",
                table: "chat_room",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_room_seller_id",
                table: "chat_room",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_my_service_service_id",
                table: "my_service",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_my_service_user_id",
                table: "my_service",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_my_voucher_user_id",
                table: "my_voucher",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_my_voucher_voucher_id",
                table: "my_voucher",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_user_id",
                table: "notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_offer_buyer_id",
                table: "offer",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_offer_product_id",
                table: "offer",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_auction_id",
                table: "Order",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_offer_id",
                table: "Order",
                column: "offer_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_product_id",
                table: "Order",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_seller_id",
                table: "Order",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_user_id",
                table: "Order",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_voucher_id",
                table: "Order",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_order_id",
                table: "payment",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_user_id",
                table: "payment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_category_id",
                table: "product",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_seller_id",
                table: "product",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_attribute_id",
                table: "product_attribute",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_product_id",
                table: "product_attribute",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_image_image_id",
                table: "product_image",
                column: "image_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_request_order_id",
                table: "refund_request",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_request_user_id",
                table: "refund_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_reviewer_id",
                table: "review",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "IX_review_seller_id",
                table: "review",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "review_order_id_reviewer_id_key",
                table: "review",
                columns: new[] { "order_id", "reviewer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_category_id",
                table: "user_favorite",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_user_id",
                table: "user_favorite",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_follow_followed_user_id",
                table: "user_follow",
                column: "followed_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_follow_follower_id",
                table: "user_follow",
                column: "follower_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_report_reporter_id",
                table: "user_report",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_report_reviewed_by",
                table: "user_report",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_search_category_id",
                table: "user_search",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_search_user_id",
                table: "user_search",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_seller_id",
                table: "voucher",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_user_id",
                table: "wishlist",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_item_product_id",
                table: "wishlist_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_item_wishlist_id",
                table: "wishlist_item",
                column: "wishlist_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_role");

            migrationBuilder.DropTable(
                name: "address");

            migrationBuilder.DropTable(
                name: "auction_deposit");

            migrationBuilder.DropTable(
                name: "banner");

            migrationBuilder.DropTable(
                name: "bid");

            migrationBuilder.DropTable(
                name: "category_image");

            migrationBuilder.DropTable(
                name: "chat");

            migrationBuilder.DropTable(
                name: "my_service");

            migrationBuilder.DropTable(
                name: "my_voucher");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "product_attribute");

            migrationBuilder.DropTable(
                name: "product_image");

            migrationBuilder.DropTable(
                name: "refund_request");

            migrationBuilder.DropTable(
                name: "review");

            migrationBuilder.DropTable(
                name: "user_favorite");

            migrationBuilder.DropTable(
                name: "user_follow");

            migrationBuilder.DropTable(
                name: "user_report");

            migrationBuilder.DropTable(
                name: "user_search");

            migrationBuilder.DropTable(
                name: "wishlist_item");

            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "chat_room");

            migrationBuilder.DropTable(
                name: "service_subscription");

            migrationBuilder.DropTable(
                name: "attributes");

            migrationBuilder.DropTable(
                name: "image");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "wishlist");

            migrationBuilder.DropTable(
                name: "auction");

            migrationBuilder.DropTable(
                name: "offer");

            migrationBuilder.DropTable(
                name: "voucher");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
