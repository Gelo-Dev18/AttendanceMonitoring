
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
    if (userID !== undefined && userID !== null) {
        url = url + userID;
    }
    $.ajax({
        
        //url: url + userID,
        url: url,
        type: 'GET',
        success: function (html) {
            modal.find('#ViewModalBody').html(html);
            $('#dataTable2').DataTable({
                "autoWidth": false, // PREVENTION SA WIDE TABLE
                "responsive": true,
                "destroy": true // SAFETY NET: Wasakin ang luma bago gumawa ng bago
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading add teacher modal:', error);
            modal.find('#AddModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
        }
    });
}

function loadArchiveViewModal(url, userID, modal) {
    if (userID !== undefined && userID !== null) {
        url = url + userID;
    }
    $.ajax({

        //url: url + userID,
        url: url,
        type: 'GET',
        success: function (html) {
            modal.find('#ArchiveViewModalBody').html(html);
            $('#dataTable2').DataTable({
                "autoWidth": false, // PREVENTION SA WIDE TABLE
                "responsive": true,
                "destroy": true // SAFETY NET: Wasakin ang luma bago gumawa ng bago
            });
        },
        error: function (xhr, status, error) {
            console.error('Error loading add teacher modal:', error);
            modal.find('#ArchiveModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
        }
    });
}

//function checkGrade() {
//    const selectElement = $('#gradeId');
//    const inputElement = $('#TVLInput');

//    if (selectElement.val() === '11') {
//        inputElement.prop('disabled', false);
//    } else {
//        inputElement.prop('disabled', true).val('');
//    }
//}
//function togglePassword() {
//    const input = document.getElementById('passwordInput');
//    const icon = document.querySelector('#toggleBtn i');
    
//    if (input.type === 'password') {
//        input.type = 'text';
//        icon.classList.remove('fa-eye');
//        icon.classList.add('fa-eye-slash');
//    } else {
//        input.type = 'password';
//        icon.classList.remove('fa-eye-slash');
//        icon.classList.add('fa-eye');
//    }
//}
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
    //$('#togglePassword').click(function (e) {
    //    e.preventDefault();

    //    var passwordInput = $('input[asp-for="NewPassword"]');
    //    var icon = $(this).find('span');

    //    if (passwordInput.attr('type') === 'password') {
    //        passwordInput.attr('type', 'text');
    //        icon.removeClass('fa-eye').addClass('fa-eye-slash');
    //    } else {
    //        passwordInput.attr('type', 'password');
    //        icon.removeClass('fa-eye-slash').addClass('fa-eye');
    //    }
    //});

    $(document).on('click', '.toggle-password', function (e) {
        e.preventDefault();
        var button = $(this);
        var passwordInput = button.closest('.input-group').find('.password-input');
        var icon = button.find('span');

        if (passwordInput.attr('type') === 'password') {
            passwordInput.attr('type', 'text');
            icon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            passwordInput.attr('type', 'password');
            icon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

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

    $(document).on('input', '#year', function () {
        let value = $(this).val();
        const cursorPostion = this.selectionStart;
        const previousValue = $(this).data('previousValue') || '';

        value = value.replace(/[^0-9\-]/g, '');
        // Split the input into year parts (e.g., "2024-")
        const yearParts = value.split('-');

        //Only auto-generate if use is typing
        const isTyping = value.length > previousValue.length;

        // Auto-generate the next year if the first year has 4 digits and the hyphen is not present
        if (yearParts[0].length === 4 && !isNaN(yearParts[0]) && yearParts.length === 1 && isTyping) {
            const nextYear = parseInt(yearParts[0]) + 1; // Add 1 to the year
            value = yearParts[0] + '-' + nextYear; // Auto-fill the second year
        }

        // Limit the second year to 4 digits if it exists
        if (yearParts[1] && yearParts[1].length > 4) {
            yearParts[1] = yearParts[1].slice(0, 4); // Truncate the second year
            value = yearParts.join('-');
        }

        // Limit the total input to 9 characters (YYYY-YYYY)
        if (value.length > 9) {
            value = value.slice(0, 9);
        }


        $(this).val(value);
        $(this).data('previousValue', value);

    });

    ///////////////////////////////////////////////////////////////////////////////
    //$(document).on('change', '#gradeId', checkGrade); //handle onchange using jqeury so no need onchange inside html tags
    //$(document).on('change', '#trackId', checkGrade);

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
                //FOr select2 plugin
                $('#sectionSelection').select2({
                    placeholder: 'Select Grade and Section...',
                    allowClear: true,
                    width: '100%',
                    minimumResultsForSearch: 0, // ALWAYS show search box
                    dropdownParent: $('#EditModal'),// para gumana yung searchbox
                    theme: 'bootstrap4'
                });
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

    $('#ManageAccountModalForm').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);
        modal.find('#ManageAccountModalFormLabel').text(title);

        loadBlurBackground();
        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                modal.find('#ManageAccountModalFormBody').html(html);
            },
            error: function (xhr, status, error) {
                console.error('Error loading edit modal:', error);
                modal.find('#EditModalBody').html('<p class="text-center">Failed to Load modal body</p>');
            }
        });
    });


    $('#ManageAccountModalForm').on('hide.bs.modal', function () {
        hideBlurBackground();
    }); 

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //AssignModal
    $('#AssignModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        var url = button.data('url');
        var title = button.data('title');
        var teacherid = button.data('id');

        var modal = $(this);

        modal.find('#AssignModalLabel').text(title);
        loadBlurBackground();

        var ajaxUrl = teacherid ? url + teacherid : url;
        $.ajax({
            url: ajaxUrl,
            type: 'GET',
            data: teacherid ? { teacherid: teacherid } : {},
            success: function (html) {
                modal.find('#AssignModalBody').html(html);
                //$('#dataTable2').DataTable();
                $('#assignTable').DataTable({

                    "autoWidth": false, // PREVENTION SA WIDE TABLE
                    "responsive": true,
                    "destroy": true // SAFETY NET: Wasakin ang luma bago gumawa ng bago
                });

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

    //Old AssignModal
    //$('#AssignModal').on('show.bs.modal', function (event) {
    //    var button = $(event.relatedTarget);

    //    var url = button.data('url');
    //    var title = button.data('title');
    //    var teacherId = button.data('id');

    //    var modal = $(this);

    //    modal.find('#AssignModalLabel').text(title);
    //    loadBlurBackground();
    //    $.ajax({
    //        url: url,
    //        type: 'GET',
    //        //data: { teacherId: teacherId },
    //        success: function (html) {
    //            modal.find('#AssignModalBody').html(html);
    //            $('#dataTable2').DataTable();

    //        },
    //        error: function (xhr, status, error) {
    //            console.error('Error loading add teacher modal:', error);
    //            modal.find('#AssignModalBody').html('<p class="text-center">Failed to load modal body</p>');
    //        }
    //    });
    //});

    //$('#AssignModal').on('hide.bs.modal', function () {
    //    hideBlurBackground();
    //});


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
                $('#dataTable3').DataTable({
                    "autoWidth": false, // PREVENTION SA WIDE TABLE
                    "responsive": true,
                    "destroy": true 
                });
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


        //For Restore only
        //$.ajax({
        //    url: url,
        //    type: 'GET',
        //    success: function (response) {
        //        modal.find('#ViewModalBody').html(response); // Load content

        //        // NOW initialize DataTable after content is loaded
        //        $('#dataTable2').DataTable();
        //    }
        //});


        loadBlurBackground();

        //if (userID !== undefined && userID !== null) {
        //    url = url + userID;
        //}
        // Ipasa 'yung local variables sa function  
        loadViewModal(url, userID, modal);

    });

    $('#ViewModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    });
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //View Small Modal

    //Trigger View Modal
    $('#ArchiveViewModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        userID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);

        modal.find('#ArchiveViewModalLabel').text(title);


        if (userID !== undefined && userID !== null) {
            url = url + userID;
        }

        loadBlurBackground();

        loadArchiveViewModal(url, userID, modal);

    });

    $('#ArchiveViewModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    });
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    $('#ViewSmallModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        userID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);

        if (!userID) {
            alert('Id does not found');
            return;
        }

        modal.find('#ViewSmallModalLabel').text(title);
        loadBlurBackground();
        $.ajax({
            url: url + userID,
            type: 'GET',
            success: function (html) {
                modal.find('#ViewSmallModalBody').html(html);

                $('#sectionSelection').select2({
                    placeholder: 'Select Grade and Section...',
                    allowClear: true,
                    width: '100%',
                    minimumResultsForSearch: 0, // ALWAYS show search box
                    dropdownParent: $('#ViewSmallModal'),// para gumana yung searchbox
                    theme: 'bootstrap4'
                });

            },
            error: function (xhr, status, error) {
                console.error('Error loading view modal:', error);
                modal.find('#AddSmallModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
            }
        });
    });

    $('#ViewSmallModal').on('hide.bs.modal', function () {
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

    //Triggers ModalDelete
    $('#ModalDefault').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        userID = button.data('id');

        loadBlurBackground();
    });

    $('#ModalDefault').on('hide.bs.modal', function (event) {
        hideBlurBackground();
    });
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    
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

    //For Promote
    $('#PromoteModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        var url = button.data('url');
        var title = button.data('title');
        var modal = $(this);

        if (button.attr('id') === 'bulkPromoteBtn') {
            const selectedIds = $('.student-checkbox:checked')
                .map(function () {
                    return $(this).data('student-id');
                })
                .get();

            if (selectedIds.length === 0) {
                showDangerToast('No students selected');
                return;
            }

            userID = selectedIds.join(',');
        } else {
            userID = button.data('id');

            if (!userID) {
                showDangerToast('Id does not found');
                return;
            }
        }

        modal.find('#PromoteModalLabel').text(title);
        loadBlurBackground();

        $.ajax({
            url: url + userID,
            type: 'GET',
            success: function (html) {
                modal.find('#PromoteModalBody').html(html);

                $('#sectionSelection').select2({
                    placeholder: 'Select Grade and Section...',
                    allowClear: true,
                    width: '100%',
                    minimumResultsForSearch: 0, // ALWAYS show search box
                    dropdownParent: $('#PromoteModal'),// para gumana yung searchbox
                    theme: 'bootstrap4'
                });

            },
            error: function (xhr, status, error) {
                console.error('Error loading view modal:', error);
                modal.find('#AddSmallModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
            }
        });
    });
    $('#PromoteModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    });
});