$(document).ready(function () {
    $('#btnFile').click(function () {
        this.value = null;
    });
    $('#btnFile').change(function () {
        $('#lblLoading').show();
        $('#frmFile').submit();
    });

    $('#btnDownload').hide();
    $('#btnDownload').click(function () {
        window.location = $('#downFN').val();
    });

    var i = 1;
    $('#btnAddRule').click(function () {
        $('#tr' + i).html(
            '<td class="Col1">IF ' +
                '<select name="colSource' + i + '">' + colsOpts + '</select> ' +
                '<select name="op' + i + '">' + opsOpts + '</select> ' +
                '<input type="text" class="txtIfValue" name="txtIfValue' + i + '" /> ' +
                'THEN transform ' +
                '<select name="colTarget' + i + '">' + colsOpts + '</select> ' +
                'To ' +
            '</td>' +
            '<td class="Col2">' +
                '<input type="text" name="txtTransfTo' + i + '" />' +
            '</td>');
        $('#tblRules').append('<tr id="tr' + (i + 1) + '"></tr>');
        i++;
    });
    $('#btnRemoveRule').click(function () {
        if (i > 1) {
            $('#tr' + (i - 1)).html('');
            i--;
        }
    });

    $('#btnApplyRules').click(function () {
        if ($('#txtTransfTo0').val() == '') {
            alert("Need to enter at least one transformation rule.")
            return;
        }

        $('#frmRules').submit();
    });

    $('#frmRules').on('submit', function (evt) {
        evt.preventDefault();   // avoid to execute the actual submit of the form.

        var dataToPost = $('#frmRules').serialize();

        $.post("/Process/ApplyRules", dataToPost)
            .done(function (response, status, jqxhr) {
                var jsonData = JSON.parse(jqxhr.responseText);
                var cols = Object.keys(jsonData[0]).length;
                var rows = jsonData.length;

                $('#tblResult > thead').empty();
                $('#tblResult > tbody').empty();

                var ths = "";
                for (var c = 0; c < cols; c++) {
                    if (c == 0) {
                        ths = ths + "<th class=\"NoTh\">" + eval("jsonData[0].c" + c) + "</th>";
                    } else {
                        ths = ths + "<th>" + eval("jsonData[0].c" + c) + "</th>";
                    }
                }

                $('#thResult').append("<tr>" + ths + "</tr>");

                for (var r = 1; r < rows; r++) {
                    tds = "";
                    for (var c = 0; c < cols; c++) {
                        if (c == 0) {
                            tds = tds + "<td class=\"NoTh\">" + eval("jsonData[" + r + "].c" + c) + "</td>";
                        } else {
                            tds = tds + "<td>" + eval("jsonData[" + r + "].c" + c) + "</td>";
                        }
                    }

                    $('#tbResult').append("<tr>" + tds + "</tr>");
                }

                $('#btnDownload').show();
            })
            .fail(function (jqxhr, status, error) {
                // this is the ""error"" callback
                alert("Server call failed: " + error);
            });
    });

    $('#tblRaw').on('scroll', function () {
        $('#tblRaw > *').width($('#tblRaw').width() + $('#tblRaw').scrollLeft());
    });
    $('#tblResult').on('scroll', function () {
        $('#tblResult > *').width($('#tblResult').width() + $('#tblResult').scrollLeft());
    });
});
