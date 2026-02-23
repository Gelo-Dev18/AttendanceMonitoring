$(document).ready(function () {
    $('.remove-button').on('click', function (e) {
        e.preventDefault();
        var button = $(this);
        //var button = $(event.relatedTarget); // ginagamit lang kapag modal

        var removeid = button.data('remove-id')

        $.ajax({
            url: '/Teacher/RemoveStudent/' + removeid,
            type: 'DELETE',
            dataType: 'json',
            success: function (response) {

                if (response.success) {
                    showSuccessToast(response.message);
                    //loadSpinner();
                    setTimeout(function () {
                        location.reload();
                    }, 1000);
                } else {
                    alert('Could not remove assign');
                }
                //alert(response.message);
                //location.reload();
            },
            error: function (xhr, status, error) {
                console.error('Error removing student assignment:', error);
                //    alert('Something went wrong. Please try again.');
                loadSpinner();
                loadPageBlur();
                showDangerToast(response);
            }
        });
    });
});