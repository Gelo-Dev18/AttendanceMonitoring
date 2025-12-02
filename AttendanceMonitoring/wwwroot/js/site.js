
//no need this dahil isang global o general naming nalang ng variable ang gagamitin which is yung userID
//var teacherID;
//var secretaryID;
//var studentID;

//Eto na ang need dahi isa isa lang naman binubuksan ang modal
var userID;
//Reusable Codes
function showSuccessToast(message) {
    //$('#successToast .toast-body').text(message);
    $('#toast-message').text(message);
    $('#successToast').toast('show');
}
function showUpdateSuccessToast(message) {
    $('#update-toast-message').text(message);
    $('#updateSuccessToast').toast('show');
}

function showDangerToast(message) {
    $('#dangertoast-message').text(message);
    $('#dangerToast').toast('show');
}

function loadSpinner() {
    $('#spinnerWrapper').removeClass("d-none");
}

function loadPageBlur() {
    $('#blurred-overlay-page').css('display', 'block');
}

function loadBlurBackground() {
    $('#blurred-overlay').css('display', 'block');
}

function hideBlurBackground() {
    $('#blurred-overlay').css('display', 'none');
}
//BEst approach Separate Function with Parameters (soc)"Separation of Concerns"
//yung function, dapat tumatanggap siya ng parameters
function loadViewModal(url, userID, modal) {
    $.ajax({
        url: url + userID,
        type: 'GET',
        success: function (html) {
            modal.find('#ViewModalBody').html(html);
        },
        error: function (xhr, status, error) {
            console.error('Error loading add teacher modal:', error);
            modal.find('#AddModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
        }
    });
}

function checkOption() {
    const selectElement = $('#categoryId');
    const inputElement = $('#TVLInput');

    if (selectElement.val() === 'TVL') {
        inputElement.prop('disabled', false);
    } else {
        inputElement.prop('disabled', true).val('');
    }
}

$(document).ready(function () {

    //Event delegation for file input change
    $(document).on('change', 'input[type="file"].custom-file-input', function () {
        var input = this;
        //Update the custom file label with selected filename
        var fileName = input.files[0] ? input.files[0].name : 'Choose file'; //ternary operator(shorthand for if else)
        var label = $(input).next('.custom-file-label');
        label.text(fileName);

        //Display image when files is selected
        if (input.files && input.files[0]) { //0 Indicates always for true
            var reader = new FileReader();
            reader.onload = function (e) {
                //Find the image in the same modal/container
                $(input).closest('.modal-content, .container').find('#imagePreview').attr('src', e.target.result);
            }
            reader.readAsDataURL(input.files[0]);
        }
    });

    ///////////////////////////////////////////////////////////////////////////////

    //Pag Binago  yung dropdown  mag-enable/disable yung TVL input
    $(document).on('change', '#categoryId', checkOption); //handle onchange using jqeury so no need onchange inside html tags
    $(document).on('change', '#trackId', checkOption);

    //Triggers Add user modal
    $('#AddModalForm').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);

        modal.find('#AddModalFormLabel').text(title);
        loadBlurBackground();
        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                modal.find('#AddModalFormBody').html(html);

                //FOr select2 plugin
                $('#sectionSelection').select2({
                    placeholder: 'Select Grade and Section...',
                    allowClear: true,
                    width: '100%',
                    minimumResultsForSearch: 0, // ALWAYS show search box
                    dropdownParent: $('#AddModalForm'),// para gumana yung searchbox
                    theme: 'bootstrap4'
                });
                checkOption(); //onchange function

            },
            error: function (xhr, status, error) {
                console.error('Error loading add teacher modal:', error);
                modal.find('#AddModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
            }
        });
    });

    $('#AddModalForm').on('hide.bs.modal', function () {
        hideBlurBackground();
    });

    //////////////////////////////////////////////////////////////////////////////

    //Triggers Edit Modal
    $('#EditModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        userID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);
        modal.find('#EditModalLabel').text(title);

        loadBlurBackground();

        if (!userID) {
            alert('Id does not found');
            return;
        }

        $.ajax({
            url: url + userID,
            type: 'GET',
            success: function (html) {
                modal.find('#EditModalBody').html(html);
                //$('#dataTable2').DataTable();

                //FOr select2 plugin
                //$('#sectionSelection').select2({
                //    placeholder: 'Select Grade and Section...',
                //    allowClear: true,
                //    width: '100%',
                //    minimumResultsForSearch: 0, // ALWAYS show search box
                //    dropdownParent: $('#EditModalForm'),// para gumana yung searchbox
                //    theme: 'bootstrap4'
                //});
            },
            error: function (xhr, status, error) {
                console.error('Error loading edit modal:', error);
                modal.find('#EditModalBody').html('<p class="text-center">Failed to Load modal body</p>');
            }
        });
    });

    

    $('#EditModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    }); 

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //AssignModal
    $('#AssignModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        var url = button.data('url');
        var title = button.data('title');
        var teacherId = button.data('id');

        var modal = $(this);

        modal.find('#AssignModalLabel').text(title);
        loadBlurBackground();
        $.ajax({
            url: url + teacherId,
            type: 'GET',
            data: { teacherId: teacherId },
            success: function (html) {
                modal.find('#AssignModalBody').html(html);
                $('#dataTable2').DataTable();

            },
            error: function (xhr, status, error) {
                console.error('Error loading add teacher modal:', error);
                modal.find('#AssignModalBody').html('<p class="text-center">Failed to load modal body</p>');
            }
        });
    });

    $('#AssignModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    });


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Triggers View Assign Modal
    $('#ViewAssignModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        userID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);
        modal.find('#ViewAssignModalLabel').text(title);

        loadBlurBackground();

        if (!userID) {
            alert('Id does not found');
            return;
        }

        $.ajax({
            url: url + userID,
            type: 'GET',
            success: function (html) {
                modal.find('#ViewAssignModalBody').html(html);
                $('#dataTable3').DataTable();
            },
            error: function (xhr, status, error) {
                console.error('Error loading edit modal:', error);
                modal.find('#ViewAssignModalBody').html('<p class="text-center">Failed to Load modal body</p>');
            }
        });
    });



    $('#ViewAssignModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    }); 

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Trigger View Modal
    $('#ViewModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        userID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);

        modal.find('#ViewModalLabel').text(title);



        loadBlurBackground();
        // Ipasa 'yung local variables sa function
        loadViewModal(url, userID, modal);
    });

    $('#ViewModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    });

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Triggers ModalDelete
    $('#ModalDelete').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        userID = button.data('id');

        loadBlurBackground();
    });

    $('#ModalDelete').on('hide.bs.modal', function (event) {
        hideBlurBackground();
    });
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Add Small Modal Form
    $('#AddSmallModalForm').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);

        modal.find('#AddSmallModalFormLabel').text(title);
        loadBlurBackground();
        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                modal.find('#AddSmallModalFormBody').html(html);
            },
            error: function (xhr, status, error) {
                console.error('Error loading add teacher modal:', error);
                modal.find('#AddSmallModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
            }
        });
    });

    $('#AddSmallModalForm').on('hide.bs.modal', function () {
        hideBlurBackground();
    });

    //Add Small Modal Form
    $('#AssignForm').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        var url = button.data('url');
        var title = button.data('title');
        var sectionId = button.data('section-id');

        var modal = $(this);

        modal.find('#AssignFormLabel').text(title);
        loadBlurBackground();
        $.ajax({
            url: url,
            type: 'GET',
            data: { sectionId: sectionId },
            success: function (html) {
                modal.find('#AssignFormBody').html(html);
            },
            error: function (xhr, status, error) {
                console.error('Error loading add teacher modal:', error);
                modal.find('#AssignFormBody').html('<p class="text-center">Failed to load modal body</p>');
            }
        });
    });

    $('#AssignForm').on('hide.bs.modal', function () {
        hideBlurBackground();
    });
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //Triggers Edit Small Modal
    $('#EditSmallModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        userID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);
        modal.find('#EditSmallModalLabel').text(title);

        loadBlurBackground();

        if (!userID) {
            alert('Id does not found');
            return;
        }

        $.ajax({
            url: url + userID,
            type: 'GET',
            success: function (html) {
                modal.find('#EditSmallModalBody').html(html);
                checkOption(); //onchange function

            },
            error: function (xhr, status, error) {
                console.error('Error loading edit modal:', error);
                modal.find('#EditSmallModalBody').html('<p class="text-center">Failed to Load modal body</p>');
            }
        });
    });

    $('#EditSmallModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    }); 

    

});