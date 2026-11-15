using System.Collections.Generic;
using NinOS.Domain;
using NinOS.Domain.ViewModels;

namespace NinOS.Tests.Dominio;

public class NoteSnapshotStoreTests
{
    private static note_print_dto crear_dto_de_prueba()
    {
        return new note_print_dto
        {
            id_delivery_note = 42,
            note_number = "000_042",
            company_name = "Empresa de Prueba",
            seller_name = "Sandra",
            customer_business_name = "CLIENTE TEST",
            customer_rif = "J-12345678-9",
            gross_total_usd = 100m,
            discount_percentage = 10m,
            discounted_total_usd = 90m,
            total_amount_usd = 90m,
            credit_days_text = "30 dias",
            conditions_text = "Condiciones de prueba",
            details = new List<note_detail_print_dto>
            {
                new note_detail_print_dto
                {
                    code = "PROD001",
                    name = "Producto de prueba",
                    quantity = 5,
                    unit_price_usd = 20m,
                    subtotal_usd = 100m
                }
            }
        };
    }

    [Fact]
    public void caso_feliz_dto_sale_igual_despues_de_serializar_y_deserializar()
    {
        note_print_dto dto_original = crear_dto_de_prueba();

        string? json = note_snapshot_store.serialize(dto_original);

        Assert.NotNull(json);
        Assert.Contains("000_042", json);

        note_print_dto? dto_recuperado = note_snapshot_store.deserialize(json);

        Assert.NotNull(dto_recuperado);
        Assert.Equal(dto_original.id_delivery_note, dto_recuperado!.id_delivery_note);
        Assert.Equal(dto_original.note_number, dto_recuperado.note_number);
        Assert.Equal(dto_original.gross_total_usd, dto_recuperado.gross_total_usd);
        Assert.Equal(dto_original.discounted_total_usd, dto_recuperado.discounted_total_usd);
        Assert.Single(dto_recuperado.details);
        Assert.Equal(dto_original.details[0].code, dto_recuperado.details[0].code);
    }

    [Fact]
    public void caso_limite_json_sin_un_campo_se_lee_sin_excepcion_y_sale_vacio()
    {
        string json_incompleto = "{\"note_number\":\"000_001\",\"company_name\":\"Empresa\"}";

        note_print_dto? dto = note_snapshot_store.deserialize(json_incompleto);

        Assert.NotNull(dto);
        Assert.Equal("000_001", dto!.note_number);
        Assert.Equal(0m, dto.gross_total_usd);
        Assert.Empty(dto.details);
    }

    [Fact]
    public void caso_limite_json_con_campo_desconocido_se_ignora_sin_excepcion()
    {
        string json_con_extra = "{\"note_number\":\"000_002\",\"campo_que_no_existe\":\"valor raro\",\"gross_total_usd\":55.00}";

        note_print_dto? dto = note_snapshot_store.deserialize(json_con_extra);

        Assert.NotNull(dto);
        Assert.Equal("000_002", dto!.note_number);
        Assert.Equal(55m, dto.gross_total_usd);
    }

    [Fact]
    public void caso_error_serializar_null_devuelve_null()
    {
        string? json = note_snapshot_store.serialize(null);
        Assert.Null(json);
    }

    [Fact]
    public void caso_error_deserializar_null_devuelve_null()
    {
        note_print_dto? dto = note_snapshot_store.deserialize(null);
        Assert.Null(dto);
    }

    [Fact]
    public void caso_error_deserializar_json_invalido_devuelve_null_sin_excepcion()
    {
        note_print_dto? dto = note_snapshot_store.deserialize("esto no es json {{{{");
        Assert.Null(dto);
    }

    [Fact]
    public void version_actual_es_positiva()
    {
        Assert.True(note_snapshot_store.CurrentVersion > 0);
    }
}
